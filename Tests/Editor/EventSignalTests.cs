// <copyright file="EventSignalTests.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace AillieoUtils.Reactive.Tests
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;

    public sealed class EventSignalTests
    {
        [Test]
        public void BatchDeliversEveryEventInOrder()
        {
            var events = Rx.Event<int>();
            IEventStream<int> stream = events;
            var received = new List<int>();
            using (stream.Subscribe(received.Add))
            {
                Rx.Batch(() =>
                {
                    events.Emit(1);
                    events.Emit(2);
                    events.Emit(3);
                });
            }

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, received);
        }

        [Test]
        public void DisposedPendingSubscriberDoesNotReceiveCurrentEvent()
        {
            var events = new EventSignal<int>();
            var received = new List<string>();
            IDisposable pending = null;
            events.Subscribe(value =>
            {
                received.Add($"first:{value}");
                pending.Dispose();
            });
            pending = events.Subscribe(value => received.Add($"pending:{value}"));

            events.Emit(7);

            CollectionAssert.AreEqual(new[] { "first:7" }, received);
        }

        [Test]
        public void SubscriberAddedDuringEmissionStartsWithNextEvent()
        {
            var events = new EventSignal<int>();
            var received = new List<string>();
            IDisposable added = null;
            events.Subscribe(value =>
            {
                received.Add($"first:{value}");
                added = added ?? events.Subscribe(next => received.Add($"added:{next}"));
            });

            events.Emit(1);
            events.Emit(2);

            CollectionAssert.AreEqual(new[] { "first:1", "first:2", "added:2" }, received);
        }

        [Test]
        public void HandlerFailureDoesNotPreventRemainingHandlers()
        {
            var events = new EventSignal<int>();
            var received = new List<int>();
            events.Subscribe(_ => throw new InvalidOperationException("first"));
            events.Subscribe(received.Add);

            var error = Assert.Throws<AggregateException>(() => events.Emit(4));

            Assert.AreEqual("first", error.InnerExceptions[0].Message);
            CollectionAssert.AreEqual(new[] { 4 }, received);
        }

        [Test]
        public void ScopeDisposalStopsEventDelivery()
        {
            var events = new EventSignal<int>();
            var scope = new ReactiveScope(null);
            var received = new List<int>();
            events.Subscribe(received.Add).AddTo(scope);
            events.Emit(1);

            scope.Dispose();
            events.Emit(2);

            CollectionAssert.AreEqual(new[] { 1 }, received);
        }

        [Test]
        public void NestedEventsAreDeliveredDepthFirstWithoutLoss()
        {
            var events = new EventSignal<int>();
            var received = new List<string>();
            events.Subscribe(value =>
            {
                received.Add($"A:{value}");
                if (value == 1)
                {
                    events.Emit(2);
                }
            });
            events.Subscribe(value => received.Add($"B:{value}"));

            events.Emit(1);

            CollectionAssert.AreEqual(new[] { "A:1", "A:2", "B:2", "B:1" }, received);
        }

        [Test]
        public void StableEmissionDoesNotAllocate()
        {
            var events = new EventSignal<int>();
            int sum = 0;
            using (events.Subscribe(value => sum += value))
            {
                for (int i = 0; i < 200; ++i)
                {
                    events.Emit(i);
                }

                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 1000; ++i)
                {
                    events.Emit(i);
                }

                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                Assert.AreEqual(0, allocated);
                Assert.Greater(sum, 0);
            }
        }

        [Test]
        public void NestedHandlerFailuresAreReportedAsOneFlatAggregate()
        {
            var events = new EventSignal<int>();
            events.Subscribe(value =>
            {
                if (value == 1)
                {
                    events.Emit(2);
                }
                else
                {
                    throw new InvalidOperationException("nested");
                }
            });
            events.Subscribe(value =>
            {
                if (value == 1)
                {
                    throw new ArgumentException("outer");
                }
            });

            var error = Assert.Throws<AggregateException>(() => events.Emit(1));

            Assert.AreEqual(2, error.InnerExceptions.Count);
            Assert.IsInstanceOf<InvalidOperationException>(error.InnerExceptions[0]);
            Assert.IsInstanceOf<ArgumentException>(error.InnerExceptions[1]);
        }
    }
}
