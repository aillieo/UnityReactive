// <copyright file="TimerSchedulerTests.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive.Tests
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;

    public sealed class TimerSchedulerTests
    {
        [Test]
        public void TimerEmitsOnceAfterDueTime()
        {
            var scheduler = new ManualTimerScheduler();
            var received = new List<long>();
            using (Rx.Timer(TimeSpan.FromSeconds(2), scheduler).Subscribe(received.Add))
            {
                scheduler.AdvanceBy(TimeSpan.FromSeconds(1));
                CollectionAssert.IsEmpty(received);

                scheduler.AdvanceBy(TimeSpan.FromSeconds(1));
                CollectionAssert.AreEqual(new long[] { 0 }, received);
                Assert.AreEqual(0, scheduler.PendingCount);

                scheduler.AdvanceBy(TimeSpan.FromSeconds(10));
                CollectionAssert.AreEqual(new long[] { 0 }, received);
            }
        }

        [Test]
        public void TimerCreatesIndependentScheduleForEachSubscriber()
        {
            var scheduler = new ManualTimerScheduler();
            var first = new List<long>();
            var second = new List<long>();
            var stream = Rx.Timer(TimeSpan.FromSeconds(1), scheduler);
            using (stream.Subscribe(first.Add))
            {
                scheduler.AdvanceBy(TimeSpan.FromSeconds(0.5));
                using (stream.Subscribe(second.Add))
                {
                    scheduler.AdvanceBy(TimeSpan.FromSeconds(0.5));
                    CollectionAssert.AreEqual(new long[] { 0 }, first);
                    CollectionAssert.IsEmpty(second);

                    scheduler.AdvanceBy(TimeSpan.FromSeconds(0.5));
                    CollectionAssert.AreEqual(new long[] { 0 }, second);
                }
            }
        }

        [Test]
        public void IntervalEmitsIncreasingValuesAndCanBeCancelled()
        {
            var scheduler = new ManualTimerScheduler();
            var received = new List<long>();
            var subscription = Rx.Interval(
                TimeSpan.FromSeconds(0.5),
                TimeSpan.FromSeconds(1),
                scheduler).Subscribe(received.Add);

            scheduler.AdvanceBy(TimeSpan.FromSeconds(2.5));
            subscription.Dispose();
            scheduler.AdvanceBy(TimeSpan.FromSeconds(5));

            CollectionAssert.AreEqual(new long[] { 0, 1, 2 }, received);
            Assert.AreEqual(0, scheduler.PendingCount);
        }

        [Test]
        public void SameTimeCallbacksPreserveSchedulingOrder()
        {
            var scheduler = new ManualTimerScheduler();
            var received = new List<int>();
            scheduler.Schedule(TimeSpan.FromSeconds(1), () => received.Add(1));
            scheduler.Schedule(TimeSpan.FromSeconds(1), () => received.Add(2));
            scheduler.Schedule(TimeSpan.FromSeconds(1), () => received.Add(3));

            scheduler.AdvanceTo(TimeSpan.FromSeconds(1));

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, received);
        }

        [Test]
        public void CallbackFailuresDoNotPreventOtherDueWork()
        {
            var scheduler = new ManualTimerScheduler();
            int calls = 0;
            scheduler.Schedule(
                TimeSpan.Zero,
                () => throw new InvalidOperationException("expected"));
            scheduler.Schedule(TimeSpan.Zero, () => ++calls);

            var error = Assert.Throws<AggregateException>(
                () => scheduler.AdvanceBy(TimeSpan.Zero));

            Assert.AreEqual("expected", error.InnerExceptions[0].Message);
            Assert.AreEqual(1, calls);
            Assert.AreEqual(0, scheduler.PendingCount);
        }

        [Test]
        public void InvalidDurationsAreRejectedAtStreamCreation()
        {
            var scheduler = new ManualTimerScheduler();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => Rx.Timer(TimeSpan.FromTicks(-1), scheduler));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => Rx.Interval(TimeSpan.Zero, scheduler));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => Rx.Interval(TimeSpan.Zero, TimeSpan.Zero, scheduler));
            Assert.Throws<ArgumentNullException>(
                () => Rx.Timer(TimeSpan.Zero, null));
        }
    }
}
