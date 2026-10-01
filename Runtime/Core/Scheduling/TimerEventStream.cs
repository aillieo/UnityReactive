// <copyright file="TimerEventStream.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive
{
    using System;

    /// <summary>Creates one independent scheduled sequence for each subscriber.</summary>
    internal sealed class TimerEventStream : IEventStream<long>
    {
        private readonly ITimerScheduler scheduler;
        private readonly TimeSpan dueTime;
        private readonly TimeSpan period;

        internal TimerEventStream(ITimerScheduler scheduler, TimeSpan dueTime, TimeSpan period)
        {
            this.scheduler = scheduler;
            this.dueTime = dueTime;
            this.period = period;
        }

        /// <inheritdoc/>
        public IDisposable Subscribe(Action<long> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var subscription = new Subscription(this.scheduler, this.dueTime, this.period, handler);
            subscription.Start();
            return subscription;
        }

        private sealed class Subscription : IDisposable
        {
            private readonly ITimerScheduler scheduler;
            private readonly TimeSpan dueTime;
            private readonly TimeSpan period;
            private Action<long> handler;
            private IDisposable scheduled;
            private long count;

            internal Subscription(
                ITimerScheduler scheduler,
                TimeSpan dueTime,
                TimeSpan period,
                Action<long> handler)
            {
                this.scheduler = scheduler;
                this.dueTime = dueTime;
                this.period = period;
                this.handler = handler;
            }

            public void Dispose()
            {
                this.handler = null;
                var handle = this.scheduled;
                this.scheduled = null;
                handle?.Dispose();
            }

            internal void Start()
            {
                try
                {
                    this.scheduled = this.period == TimeSpan.Zero
                        ? this.scheduler.Schedule(this.dueTime, this.OnTick)
                        : this.scheduler.SchedulePeriodic(this.dueTime, this.period, this.OnTick);
                }
                catch
                {
                    this.handler = null;
                    throw;
                }
            }

            private void OnTick()
            {
                var callback = this.handler;
                if (callback == null)
                {
                    return;
                }

                long value = this.count;
                this.count = unchecked(value + 1);
                if (this.period == TimeSpan.Zero)
                {
                    this.handler = null;
                    this.scheduled = null;
                }

                callback(value);
            }
        }
    }
}
