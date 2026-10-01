// <copyright file="ReactiveCoreTests.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace AillieoUtils.Reactive.Tests
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;

    public sealed class ReactiveCoreTests
    {
        private ReactiveScope owned;

        [SetUp]
        public void Setup()
        {
            this.owned = new ReactiveScope(null); Rx.FrameScheduler.Clear(); Rx.FrameScheduler.ExceptionHandler = null;
        }

        [TearDown]
        public void Cleanup()
        {
            this.owned.Dispose(); Rx.FrameScheduler.Clear();
        }

        private ReactiveSubscription Observe(Action effect)
        {
            return this.owned.Observe(effect);
        }

        [Test]
        public void ReactiveSourcesDoNotExposeMutableSignalsPublicly()
        {
            Assert.IsNull(typeof(IReactiveSource).GetProperty("OnChanged"));
            Assert.IsNull(typeof(ReactiveProperty<int>).GetProperty("OnChanged"));
            Assert.IsNull(typeof(ComputedReactiveProperty<int>).GetProperty("OnChanged"));
            Assert.IsNull(typeof(ReactiveCollection<int>).GetProperty("OnChanged"));
            Assert.IsNull(typeof(ReactiveCollection<int>).GetProperty("OnCollectionChanged"));
            Assert.IsNull(typeof(ReactiveDictionary<int, int>).GetProperty("OnChanged"));
            Assert.IsNull(typeof(ReactiveDictionary<int, int>).GetProperty("OnCollectionChanged"));
            Assert.IsNull(typeof(ReactiveSet<int>).GetProperty("OnChanged"));
            Assert.IsNull(typeof(ReactiveSet<int>).GetProperty("OnCollectionChanged"));
        }

        [Test]
        public void ValueSuppressesEqualWritesAndSupportsComparer()
        {
            var value = new ReactiveProperty<string>("A", StringComparer.OrdinalIgnoreCase); int runs = 0;
            this.Observe(() => { _ = value.Value; ++runs; });
            value.Value = "a"; Assert.AreEqual(1, runs);
            value.Value = "b"; Assert.AreEqual(2, runs);
            value.ForceNotify();
            Assert.AreEqual(3, runs);
        }

        [Test]
        public void PropertySubscribeReplaysSynchronouslyAndDisposes()
        {
            var property = new ReactiveProperty<int>(7); var values = new List<int>();
            var subscription = property.Subscribe(values.Add).AddTo(this.owned);
            CollectionAssert.AreEqual(new[] { 7 }, values);
            property.Value = 8; CollectionAssert.AreEqual(new[] { 7, 8 }, values);
            subscription.Dispose(); property.Value = 9; Assert.AreEqual(2, values.Count);
        }

        [Test]
        public void AutomaticObserveReplacesBranchDependencies()
        {
            var branch = Rx.Property(true); var a = Rx.Property(1); var b = Rx.Property(2); int runs = 0, result = 0;
            this.Observe(() => { ++runs; result = branch.Value ? a.Value : b.Value; });
            b.Value = 3; Assert.AreEqual(1, runs);
            branch.Value = false; a.Value = 4;
            Assert.AreEqual(2, runs); Assert.AreEqual(3, result); Assert.AreEqual(0, a.OnChanged.ListenerCount);
            b.Value = 5; Assert.AreEqual(3, runs);
        }

        [Test]
        public void DefaultObserveRunsInitiallyThenCoalescesAndCancels()
        {
            var value = Rx.Property(0); int runs = 0;
            var watch = this.owned.Add(Rx.Observe(() => { _ = value.Value; ++runs; }));
            value.Value = 1; value.Value = 2; Assert.AreEqual(1, runs);
            Rx.FrameScheduler.Flush(); Assert.AreEqual(2, runs);
            value.Value = 3; watch.Dispose(); Rx.FrameScheduler.Flush(); Assert.AreEqual(2, runs);
        }

        [Test]
        public void StaticObserveTracksOnlyDeclaredInputs()
        {
            var a = Rx.Property(0); var b = Rx.Property(0); int runs = 0;
            this.owned.Add(Rx.Observe(() => { _ = b.Value; ++runs; }, new IReactiveSource[] { a, a }, null));
            b.Value++; Assert.AreEqual(1, runs); a.Value++; Assert.AreEqual(2, runs);
        }

        [Test]
        public void NestedTrackingDoesNotLeakInnerReads()
        {
            var a = Rx.Property(0); var b = Rx.Property(0); int outer = 0;
            this.Observe(() => { _ = a.Value; ++outer; this.owned.Add(Rx.Observe(() => { _ = b.Value; }, (IReactiveScheduler)null)); });
            b.Value++; Assert.AreEqual(1, outer);
        }

        [Test]
        public void UntrackedAndPostEffectReadsAreExcluded()
        {
            var a = Rx.Property(1); var b = Rx.Property(2); int runs = 0;
            this.owned.Add(Rx.Observe(() => { Rx.Untracked(() => b.Value); return a.Value; }, result => { _ = b.Value; ++runs; }, null));
            b.Value++; Assert.AreEqual(1, runs); a.Value++; Assert.AreEqual(2, runs);
        }

        [Test]
        public void PauseTrackingSupportsNestedLifoScopes()
        {
            var collector = new DependencyCollector();
            Tracking.Capture(() =>
            {
                Assert.AreSame(collector, Tracking.Current);
                using (Rx.PauseTracking())
                {
                    Assert.IsNull(Tracking.Current);
                    using (Rx.PauseTracking())
                    {
                        Assert.IsNull(Tracking.Current);
                    }

                    Assert.IsNull(Tracking.Current);
                }

                Assert.AreSame(collector, Tracking.Current);
            }, collector);
            Assert.IsNull(Tracking.Current);
        }

        [Test]
        public void PauseTrackingRejectsOutOfOrderDisposalAndCanRecover()
        {
            var collector = new DependencyCollector();
            Tracking.Capture(() =>
            {
                var outer = Rx.PauseTracking();
                var inner = Rx.PauseTracking();

                Assert.Throws<InvalidOperationException>(() => outer.Dispose());
                Assert.IsNull(Tracking.Current);

                inner.Dispose();
                outer.Dispose();
                Assert.AreSame(collector, Tracking.Current);
            }, collector);
        }

        [Test]
        public void PauseTrackingRejectsHandlesThatEscapeCaptureScope()
        {
            var collector = new DependencyCollector();
            IDisposable escaped = null;
            Tracking.Capture(() => escaped = Rx.PauseTracking(), collector);

            Assert.IsNull(Tracking.Current);
            Assert.Throws<InvalidOperationException>(() => escaped.Dispose());
            Assert.IsNull(Tracking.Current);
        }

        [Test]
        public void NestedBatchRunsOnceWithFinalValues()
        {
            var a = Rx.Property(0); var b = Rx.Property(0); int runs = 0, result = 0;
            this.Observe(() => { ++runs; result = a.Value + b.Value; });
            Rx.Batch(() => { a.Value = 1; Rx.Batch(() => { a.Value = 2; b.Value = 3; }); Assert.AreEqual(1, runs); });
            Assert.AreEqual(2, runs); Assert.AreEqual(5, result);
        }

        [Test]
        public void NestedPostEffectWaitsUntilOuterObserveHasSubscribed()
        {
            var value = Rx.Property(0); int result = -1; bool created = false;
            this.Observe(() =>
            {
                result = value.Value;
                if (created)
                {
                    return;
                }

                created = true;
                this.owned.Add(Rx.Observe(() => 1, next => value.Value = next, null));
            });
            Assert.AreEqual(1, result);
        }

        [Test]
        public void BatchRestoresStateAfterWorkerException()
        {
            var value = Rx.Property(0); int result = 0; this.Observe(() => result = value.Value);
            Assert.Throws<InvalidOperationException>(() => Rx.Batch(() => { value.Value = 1; throw new InvalidOperationException(); }));
            Assert.AreEqual(1, result); value.Value = 2; Assert.AreEqual(2, result);
        }

        [Test]
        public void ComputedIsLazyCachedAndSuppressesEqualResults()
        {
            var value = Rx.Property(0); int reductions = 0, runs = 0;
            var parity = this.owned.Add(Rx.Computed(() => { ++reductions; return value.Value % 2; }));
            Assert.AreEqual(0, reductions); Assert.AreEqual(0, parity.Value); Assert.AreEqual(0, parity.Value); Assert.AreEqual(1, reductions);
            this.Observe(() => { _ = parity.Value; ++runs; });
            value.Value = 2; Assert.AreEqual(1, runs); value.Value = 3; Assert.AreEqual(2, runs);
        }

        [Test]
        public void UnobservedComputedDefersRecalculation()
        {
            var value = Rx.Property(0); int runs = 0;
            var c = this.owned.Add(Rx.Computed(() => { ++runs; return value.Value; }));
            _ = c.Value; value.Value = 1; value.Value = 2;
            Assert.AreEqual(1, runs); Assert.AreEqual(2, c.Value); Assert.AreEqual(2, runs);
        }

        [Test]
        public void ComputedReplacesBranchDependencies()
        {
            var branch = Rx.Property(true); var a = Rx.Property(1); var b = Rx.Property(2);
            var c = this.owned.Add(Rx.Computed(() => branch.Value ? a.Value : b.Value));
            this.Observe(() => { _ = c.Value; }); branch.Value = false;
            Assert.AreEqual(0, a.OnChanged.ListenerCount); Assert.AreEqual(2, c.Value);
        }

        [Test]
        public void StaticComputedDoesNotCaptureIncidentalReads()
        {
            var a = Rx.Property(1); var b = Rx.Property(2);
            var c = this.owned.Add(Rx.Computed(() => a.Value + b.Value, a));
            Assert.AreEqual(3, c.Value); b.Value = 5; Assert.AreEqual(3, c.Value);
            a.Value = 2; Assert.AreEqual(7, c.Value);
        }

        [Test]
        public void DiamondGraphIsConsistentAndRunsEffectOnce()
        {
            var root = Rx.Property(1);
            var left = this.owned.Add(Rx.Computed(() => root.Value * 2));
            var right = this.owned.Add(Rx.Computed(() => root.Value * 3));
            var total = this.owned.Add(Rx.Computed(() => left.Value + right.Value));
            var results = new List<int>();
            this.Observe(() => { Assert.AreEqual(root.Value * 5, total.Value); results.Add(total.Value); });
            root.Value = 2;
            CollectionAssert.AreEqual(new[] { 5, 10 }, results);
        }

        [Test]
        public void ComputedReadsInsideBatchAreFresh()
        {
            var root = Rx.Property(1); var a = this.owned.Add(Rx.Computed(() => root.Value * 2));
            var b = this.owned.Add(Rx.Computed(() => a.Value + 1)); Assert.AreEqual(3, b.Value);
            Rx.Batch(() => { root.Value = 2; Assert.AreEqual(5, b.Value); });
        }

        [Test]
        public void CircularComputedFailsAndTrackingRecovers()
        {
            ComputedReactiveProperty<int> a = null, b = null;
            a = this.owned.Add(Rx.Computed(() => b.Value)); b = this.owned.Add(Rx.Computed(() => a.Value));
            Assert.Throws<InvalidOperationException>(() => { _ = a.Value; });
            var value = Rx.Property(1); int result = 0; this.Observe(() => result = value.Value);
            value.Value = 2; Assert.AreEqual(2, result);
        }

        [Test]
        public void ComputedCanRecoverFromReducerException()
        {
            var value = Rx.Property(1); var c = this.owned.Add(Rx.Computed(() => value.Value == 2 ? throw new InvalidOperationException() : value.Value));
            int result = 0; this.Observe(() => result = c.Value);
            Assert.Throws<AggregateException>(() => value.Value = 2);
            value.Value = 3; Assert.AreEqual(3, result);
        }

        [Test]
        public void ThrowingObserveDoesNotPreventOtherSubscribersOrFutureRuns()
        {
            var value = Rx.Property(0); int result = 0;
            this.Observe(() =>
            {
                if (value.Value == 1)
{
    throw new InvalidOperationException();
}
            });
            this.Observe(() => result = value.Value);
            Assert.Throws<AggregateException>(() => value.Value = 1); Assert.AreEqual(1, result);
            value.Value = 2; Assert.AreEqual(2, result);
        }

        [Test]
        public void DisposeDuringEffectIsPermanent()
        {
            var value = Rx.Property(0); int runs = 0; ReactiveSubscription watch = null;
            watch = this.Observe(() =>
            {
                ++runs; if (value.Value > 0)
{
    watch.Dispose();
}
            });
            value.Value = 1; value.Value = 2;
            Assert.AreEqual(2, runs); Assert.AreEqual(0, value.OnChanged.ListenerCount);
        }

        [Test]
        public void FailedInitialObserveDoesNotLeakSubscriptions()
        {
            var value = Rx.Property(1);
            Assert.Throws<InvalidOperationException>(() => Rx.Observe(() => { _ = value.Value; throw new InvalidOperationException(); }));
            Assert.AreEqual(0, value.OnChanged.ListenerCount);
        }

        [Test]
        public void CustomSetterControlsNotifications()
        {
            int storage = 0, runs = 0;
            var value = Rx.Custom(() => storage, next =>
            {
                if (next < 0 || next == storage)
{
    return false;
}

            storage = next; return true;
            });
            this.Observe(() => { _ = value.Value; ++runs; }); value.Value = -1; value.Value = 2;
            Assert.AreEqual(2, runs); Assert.AreEqual(2, storage);
        }

        [Test]
        public void DeferredSchedulerHandlesCancelRequeueAndExceptions()
        {
            var scheduler = new DeferredScheduler(); var results = new List<int>();
            Action second = () => results.Add(2);
            scheduler.Schedule(() => { results.Add(1); scheduler.Cancel(second); scheduler.Schedule(second); });
            scheduler.Schedule(second); scheduler.Flush(); CollectionAssert.AreEqual(new[] { 1 }, results);
            scheduler.Flush(); CollectionAssert.AreEqual(new[] { 1, 2 }, results);
            scheduler.Schedule(() => throw new InvalidOperationException()); scheduler.Schedule(() => results.Add(3));
            Assert.Throws<AggregateException>(() => scheduler.Flush()); Assert.AreEqual(3, results[2]);
        }

        [Test]
        public void ScopeClearAllowsReuseAndDisposeClosesIt()
        {
            var value = Rx.Property(0); int runs = 0; this.Observe(() => { _ = value.Value; ++runs; });
            this.owned.Clear(); value.Value++; Assert.AreEqual(1, runs);
            this.Observe(() => { _ = value.Value; ++runs; }); value.Value++; Assert.AreEqual(3, runs);
            this.owned.Dispose(); this.owned.Dispose(); value.Value++; Assert.AreEqual(3, runs);
            Assert.Throws<ObjectDisposedException>(() => this.owned.Observe(() => { }));
        }

        [Test]
        public void DisposedComputedRejectsReads()
        {
            var value = Rx.Property(1); var c = Rx.Computed(() => value.Value); _ = c.Value; c.Dispose(); c.Dispose();
            Assert.AreEqual(0, value.OnChanged.ListenerCount); Assert.Throws<ObjectDisposedException>(() => { _ = c.Value; });
        }

        [Test]
        public void SignalAllowsUnsubscribeDuringDispatch()
        {
            var signal = new Signal(); IDisposable second = null; int calls = 0;
            this.owned.Add(signal.Subscribe(() => second.Dispose())); second = this.owned.Add(signal.Subscribe(() => ++calls));
            signal.Notify(); Assert.AreEqual(0, calls);
        }

        [Test]
        public void DisposeDetachesDependenciesWhenSchedulerCancelThrows()
        {
            var source = Rx.Property(0);
            var subscription = Rx.Observe(() => { _ = source.Value; }, new ThrowingCancelScheduler());
            Assert.Throws<InvalidOperationException>(() => subscription.Dispose());
            Assert.AreEqual(0, source.OnChanged.ListenerCount);
        }

        private sealed class ThrowingCancelScheduler : IReactiveScheduler
        {
            public void Schedule(Action effect)
            {
            }

            public void Cancel(Action effect)
            {
                throw new InvalidOperationException("cancel failed");
            }
        }
    }
}
