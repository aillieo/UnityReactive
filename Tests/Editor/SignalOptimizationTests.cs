// <copyright file="SignalOptimizationTests.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace AillieoUtils.Reactive.Tests
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;

    public sealed class SignalOptimizationTests
    {
        [Test]
        public void CancelNextDuringTraversalPreservesRemainingChain()
        {
            var signal = new Signal();
            var calls = new List<string>();
            IDisposable middle = null;
            using (signal.Subscribe(() => { calls.Add("A"); middle.Dispose(); }, true))
            using (middle = signal.Subscribe(() => calls.Add("B"), true))
            using (signal.Subscribe(() => calls.Add("C"), true))
            {
                signal.Notify();
                CollectionAssert.AreEqual(new[] { "A", "C" }, calls);
                Assert.AreEqual(2, signal.ListenerCount);
            }

            Assert.AreEqual(0, signal.ListenerCount);
        }

        [Test]
        public void ReplacementDoesNotJoinCurrentTraversal()
        {
            var signal = new Signal();
            var calls = new List<string>();
            IDisposable middle = null, added = null;
            using (signal.Subscribe(
                () =>
            {
                calls.Add("A");
                if (added == null)
                {
                    middle.Dispose();
                    added = signal.Subscribe(() => calls.Add("D"), true);
                }
            }, true))
            using (middle = signal.Subscribe(() => calls.Add("B"), true))
            using (signal.Subscribe(() => calls.Add("C"), true))
            {
                try
                {
                    signal.Notify();
                    CollectionAssert.AreEqual(new[] { "A", "C" }, calls);
                    calls.Clear();
                    signal.Notify();
                    CollectionAssert.AreEqual(new[] { "A", "C", "D" }, calls);
                }
                finally
                {
                    added?.Dispose();
                }
            }
        }

        [Test]
        public void RemovedTailStillMarksCurrentTraversalBoundary()
        {
            var signal = new Signal();
            var calls = new List<string>();
            IDisposable tail = null, added = null;
            using (signal.Subscribe(() => { calls.Add("A"); tail.Dispose(); added = signal.Subscribe(() => calls.Add("D"), true); }, true))
            using (signal.Subscribe(() => calls.Add("B"), true))
            using (tail = signal.Subscribe(() => calls.Add("C"), true))
            {
                try
                {
                    signal.Notify();
                    CollectionAssert.AreEqual(new[] { "A", "B" }, calls);
                }
                finally
                {
                    added?.Dispose();
                }
            }
        }

        [Test]
        public void NestedTraversalKeepsLinksUntilOutermostNotificationEnds()
        {
            var signal = new Signal();
            var calls = new List<string>();
            bool nested = false;
            IDisposable middle = null;
            using (signal.Subscribe(
                () =>
            {
                calls.Add("A");
                if (!nested)
                {
                    nested = true;
                    middle.Dispose();
                    signal.Notify();
                }
            }, true))
            using (middle = signal.Subscribe(() => calls.Add("B"), true))
            using (signal.Subscribe(() => calls.Add("C"), true))
            {
                signal.Notify();
                CollectionAssert.AreEqual(new[] { "A", "A", "C", "C" }, calls);
            }
        }

        [Test]
        public void SelfRemovalDoesNotSkipNextListener()
        {
            var signal = new Signal();
            int calls = 0;
            IDisposable self = null;
            using (self = signal.Subscribe(() => { ++calls; self.Dispose(); }, true))
            using (signal.Subscribe(() => ++calls, true))
            {
                signal.Notify(); signal.Notify();
                Assert.AreEqual(3, calls);
            }
        }

        [Test]
        public void QueuedDisposedListenerCannotInvokeReplacement()
        {
            var signal = new Signal();
            int oldCalls = 0, newCalls = 0;
            var old = signal.Subscribe(() => ++oldCalls);
            IDisposable replacement;
            using (Rx.Batch())
            {
                signal.Notify(); old.Dispose();
                replacement = signal.Subscribe(() => ++newCalls);
            }

            using (replacement)
            {
                Assert.AreEqual(0, oldCalls); Assert.AreEqual(0, newCalls);
                signal.Notify(); Assert.AreEqual(1, newCalls);
            }
        }

        [Test]
        public void BatchDeduplicatesEachSubscriptionIndependently()
        {
            var signal = new Signal();
            int calls = 0;
            Action callback = () => ++calls;
            using (signal.Subscribe(callback))
            using (signal.Subscribe(callback))
            {
                Rx.Batch(() => { signal.Notify(); signal.Notify(); });
                Assert.AreEqual(2, calls);
            }
        }

        [Test]
        public void SubscriberAddedAfterNotifyDoesNotReceivePendingNotification()
        {
            var signal = new Signal();
            int calls = 0;
            IDisposable added = null;
            using (Rx.Batch())
            {
                signal.Notify();
                added = signal.Subscribe(() => ++calls);
            }

            using (added)
            {
                Assert.AreEqual(0, calls);
                signal.Notify();
                Assert.AreEqual(1, calls);
            }
        }

        [Test]
        public void SubscriberAddedBetweenTwoBatchedNotificationsReceivesLatestOnce()
        {
            var signal = new Signal();
            int calls = 0;
            IDisposable added = null;
            using (Rx.Batch())
            {
                signal.Notify();
                added = signal.Subscribe(() => ++calls);
                signal.Notify();
            }

            using (added)
            {
                Assert.AreEqual(1, calls);
            }
        }

        [Test]
        public void NestedNormalNotificationRequeuesOnlyListenersAlreadyDelivered()
        {
            var signal = new Signal();
            var calls = new List<string>();
            bool nested = false;
            using (signal.Subscribe(() =>
            {
                calls.Add("A");
                if (!nested)
                {
                    nested = true;
                    signal.Notify();
                }
            }))
            using (signal.Subscribe(() => calls.Add("B")))
            {
                signal.Notify();
                CollectionAssert.AreEqual(new[] { "A", "B", "A" }, calls);
            }
        }

        [Test]
        public void NormalListenerExceptionsAreAggregatedWithoutStoppingSiblings()
        {
            var signal = new Signal();
            int calls = 0;
            using (signal.Subscribe(() => throw new InvalidOperationException("first")))
            using (signal.Subscribe(() => ++calls))
            using (signal.Subscribe(() => throw new ArgumentException("second")))
            {
                var error = Assert.Throws<AggregateException>(() => signal.Notify());
                Assert.AreEqual(1, calls);
                Assert.AreEqual(2, error.InnerExceptions.Count);
                Assert.IsInstanceOf<InvalidOperationException>(error.InnerExceptions[0]);
                Assert.IsInstanceOf<ArgumentException>(error.InnerExceptions[1]);
            }
        }

        [Test]
        public void NormalDispatchYieldsToComputedRefreshBeforeNextListener()
        {
            var root = Rx.Property(0);
            var dependency = Rx.Property(0);
            var calls = new List<string>();
            using (var computed = Rx.Computed(() =>
            {
                int value = dependency.Value;
                calls.Add("computed");
                return value * 2;
            }))
            using (computed.Subscribe(_ => { }))
            using (root.Subscribe(value =>
            {
                if (value == 1)
                {
                    calls.Add("A");
                    dependency.Value = 1;
                }
            }))
            using (root.Subscribe(value =>
            {
                if (value == 1)
                {
                    calls.Add("B");
                }
            }))
            {
                calls.Clear();
                root.Value = 1;
                CollectionAssert.AreEqual(new[] { "A", "computed", "B" }, calls);
            }
        }

        [Test]
        public void DirectAndNestedQueuedErrorsShareOneAggregate()
        {
            var source = new Signal();
            var nested = new Signal();
            int siblingCalls = 0;
            using (nested.Subscribe(() => throw new ArgumentException("nested")))
            using (source.Subscribe(() =>
            {
                nested.Notify();
                throw new InvalidOperationException("direct");
            }))
            using (source.Subscribe(() => ++siblingCalls))
            {
                var error = Assert.Throws<AggregateException>(() => source.Notify());
                Assert.AreEqual(1, siblingCalls);
                Assert.AreEqual(2, error.InnerExceptions.Count);
                Assert.IsInstanceOf<InvalidOperationException>(error.InnerExceptions[0]);
                Assert.IsInstanceOf<ArgumentException>(error.InnerExceptions[1]);
            }
        }

        [Test]
        public void ExceptionDuringTraversalRestoresCleanupAndBatchState()
        {
            var signal = new Signal();
            IDisposable self = null;
            self = signal.Subscribe(() => { self.Dispose(); throw new InvalidOperationException(); }, true);
            Assert.Throws<InvalidOperationException>(() => signal.Notify());
            Assert.AreEqual(0, signal.ListenerCount);
            int calls = 0;
            using (signal.Subscribe(() => ++calls))
            {
                signal.Notify();
                Assert.AreEqual(1, calls);
            }
        }

        [Test]
        public void StableAutomaticGraphDoesNotAllocateAfterWarmup()
        {
            var source = Rx.Property(0);
            int result = 0;
            using (var computed = Rx.Computed(() => source.Value * 2))
            using (Rx.Observe(() => result = computed.Value, scheduler: null))
            {
                for (int i = 0; i < 200; ++i)
                {
                    source.Value = i;
                }

                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 200; i < 1200; ++i)
                {
                    source.Value = i;
                }

                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                Assert.AreEqual(2398, result);
                Assert.AreEqual(0, allocated, "Stable computed + automatic observation must reuse notification and dependency storage.");
            }
        }

        [Test]
        public void StableCaptureHandlesRepeatedReadsAndBranchReplacement()
        {
            var useLeft = Rx.Property(true);
            var left = Rx.Property(2);
            var right = Rx.Property(5);
            int result = 0;
            using (var computed = Rx.Computed(() => useLeft.Value ? left.Value + left.Value : right.Value + right.Value))
            using (computed.Subscribe(value => result = value))
            {
                Assert.AreEqual(4, result);
                Assert.AreEqual(1, left.OnChanged.ListenerCount);
                Assert.AreEqual(0, right.OnChanged.ListenerCount);

                useLeft.Value = false;
                Assert.AreEqual(10, result);
                Assert.AreEqual(0, left.OnChanged.ListenerCount);
                Assert.AreEqual(1, right.OnChanged.ListenerCount);

                left.Value = 3;
                Assert.AreEqual(10, result);
                right.Value = 7;
                Assert.AreEqual(14, result);
            }
        }

        [Test]
        public void ComputedBatchDeduplicatesAcrossQueuedBranchReplacement()
        {
            var useLeft = Rx.Property(true);
            var left = Rx.Property(2);
            var right = Rx.Property(5);
            int evaluations = 0;
            int result = 0;
            using (var computed = Rx.Computed(() =>
            {
                ++evaluations;
                return useLeft.Value ? left.Value : right.Value;
            }))
            using (computed.Subscribe(value => result = value))
            {
                Assert.AreEqual(1, evaluations);
                Rx.Batch(() =>
                {
                    left.Value = 3;
                    useLeft.Value = false;
                    Assert.AreEqual(5, computed.Value);
                    right.Value = 7;
                });

                Assert.AreEqual(3, evaluations);
                Assert.AreEqual(7, result);
                Assert.AreEqual(0, left.OnChanged.ListenerCount);
                Assert.AreEqual(1, right.OnChanged.ListenerCount);
            }
        }

        [Test]
        public void PropertySubscribeDefersNestedPostUntilCallbackReturns()
        {
            var source = Rx.Property(0);
            var calls = new List<string>();
            using (var scope = new ReactiveScope(null))
            {
                scope.Add(source.Subscribe(value =>
                {
                    calls.Add("begin");
                    scope.Add(Rx.Observe(() => value, next => calls.Add("post"), null));
                    calls.Add("end");
                }));
                CollectionAssert.AreEqual(new[] { "begin", "end", "post" }, calls);
            }
        }

        [Test]
        public void PropertySubscribersReadLatestValueDuringNestedWrites()
        {
            var source = Rx.Property(0);
            var calls = new List<string>();
            using (source.Subscribe(value =>
            {
                calls.Add($"A:{value}");
                if (value == 1)
                {
                    source.Value = 2;
                }
            }))
            using (source.Subscribe(value => calls.Add($"B:{value}")))
            {
                calls.Clear();
                source.Value = 1;
                CollectionAssert.AreEqual(new[] { "A:1", "B:2", "A:2" }, calls);
            }
        }

        [Test]
        public void PropertySubscribeCancelsQueuedCallbacksAndCleansUpInitialFailure()
        {
            var source = Rx.Property(0);
            Assert.Throws<InvalidOperationException>(() => source.Subscribe(value => { throw new InvalidOperationException(); }));
            Assert.AreEqual(0, source.OnChanged.ListenerCount);
            int calls = 0;
            var subscription = source.Subscribe(value => ++calls);
            Rx.Batch(() => { source.Value = 1; subscription.Dispose(); });
            Assert.AreEqual(1, calls);
        }

        [Test]
        public void PropertyChangeSubscriptionReportsInitialAndPreviousValues()
        {
            var source = Rx.Property(1);
            var changes = new List<PropertyChange<int>>();

            using (source.SubscribeChanges(changes.Add))
            {
                Assert.AreEqual(1, changes.Count);
                Assert.IsFalse(changes[0].HasPreviousValue);
                Assert.AreEqual(default(int), changes[0].OldValue);
                Assert.AreEqual(1, changes[0].NewValue);

                source.Value = 2;
                Assert.AreEqual(2, changes.Count);
                Assert.IsTrue(changes[1].HasPreviousValue);
                Assert.AreEqual(1, changes[1].OldValue);
                Assert.AreEqual(2, changes[1].NewValue);

                source.ForceNotify();
                Assert.AreEqual(3, changes.Count);
                Assert.AreEqual(2, changes[2].OldValue);
                Assert.AreEqual(2, changes[2].NewValue);
            }
        }

        [Test]
        public void PropertyChangeSubscriptionReportsLastDeliveredAndFinalBatchedValues()
        {
            var source = Rx.Property(1);
            var changes = new List<PropertyChange<int>>();

            using (source.SubscribeChanges(changes.Add))
            {
                Rx.Batch(() =>
                {
                    source.Value = 2;
                    source.Value = 3;
                });

                Assert.AreEqual(2, changes.Count);
                Assert.AreEqual(1, changes[1].OldValue);
                Assert.AreEqual(3, changes[1].NewValue);
            }
        }

        [Test]
        public void PropertyChangeSubscriptionSupportsCustomProperties()
        {
            int storage = 4;
            var source = Rx.Custom(
                () => storage,
                value =>
                {
                    storage = value;
                    return true;
                });
            var changes = new List<PropertyChange<int>>();

            using (source.SubscribeChanges(changes.Add))
            {
                source.Value = 6;

                Assert.AreEqual(2, changes.Count);
                Assert.IsFalse(changes[0].HasPreviousValue);
                Assert.AreEqual(4, changes[0].NewValue);
                Assert.IsTrue(changes[1].HasPreviousValue);
                Assert.AreEqual(4, changes[1].OldValue);
                Assert.AreEqual(6, changes[1].NewValue);
            }
        }

        [Test]
        public void PropertyChangeSubscriptionCleansUpInitialFailure()
        {
            var source = Rx.Property(0);

            Assert.Throws<InvalidOperationException>(() =>
                source.SubscribeChanges(_ => throw new InvalidOperationException()));
            Assert.AreEqual(0, source.OnChanged.ListenerCount);
        }

        [Test]
        public void StablePropertyChangeNotificationsDoNotAllocate()
        {
            var source = Rx.Property(0);
            int checksum = 0;
            using (source.SubscribeChanges(change => checksum += change.NewValue))
            {
                for (int i = 1; i <= 200; ++i)
                {
                    source.Value = i;
                }

                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 201; i <= 400; ++i)
                {
                    source.Value = i;
                }

                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                Assert.AreEqual(0, allocated);
                Assert.Greater(checksum, 0);
            }
        }

        [Test]
        public void DeferredSchedulerReusesFlushStorage()
        {
            var scheduler = new DeferredScheduler();
            int calls = 0;
            Action action = () => ++calls;
            for (int i = 0; i < 200; ++i)
            {
                scheduler.Schedule(action);
                scheduler.Flush();
            }

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 200; ++i)
            {
                scheduler.Schedule(action);
                scheduler.Flush();
                scheduler.Flush();
            }

            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.AreEqual(400, calls);
            Assert.AreEqual(0, allocated);
        }

        [Test]
        public void StableLargeCollectionTrackingDoesNotAllocatePerKeyCleanupLists()
        {
            var collection = new ReactiveCollection<int>();
            for (int i = 0; i < 2048; ++i)
            {
                collection.Add(i);
            }

            var trigger = Rx.Property(0);
            long checksum = 0;
            using (Rx.Observe(
                () =>
            {
                checksum += trigger.Value;
                foreach (int value in collection)
                {
                    checksum += value;
                }
            }, scheduler: null))
            {
                _ = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 1; i <= 200; ++i)
                {
                    trigger.Value = i;
                }

                long before = GC.GetAllocatedBytesForCurrentThread();
                trigger.Value = 201;
                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

                Assert.LessOrEqual(allocated, 128);
                Assert.Greater(checksum, 0);
            }
        }

        [Test]
        public void CollectionIndexOfDoesNotAllocateACapturedPredicate()
        {
            var collection = new ReactiveCollection<int>();
            for (int i = 0; i < 128; ++i)
            {
                collection.Add(i);
            }

            int checksum = 0;
            _ = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 200; ++i)
            {
                checksum += collection.IndexOf(-1);
            }

            long before = GC.GetAllocatedBytesForCurrentThread();
            checksum += collection.IndexOf(-1);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.AreEqual(0, allocated);
            Assert.AreEqual(-201, checksum);
        }
    }
}
