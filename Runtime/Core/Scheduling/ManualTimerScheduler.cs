// <copyright file="ManualTimerScheduler.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive
{
    using System;
    using System.Collections.Generic;

    /// <summary>Provides deterministic, manually advanced time for tests and host-driven clocks.</summary>
    /// <remarks>This scheduler is single-threaded. Periodic work uses fixed-rate scheduling without catch-up limits.</remarks>
    public sealed class ManualTimerScheduler : ITimerScheduler
    {
        private readonly List<ScheduledItem> items = new List<ScheduledItem>();
        private TimeSpan now;
        private long sequence;

        /// <inheritdoc/>
        public TimeSpan Now => this.now;

        /// <summary>Gets the number of active scheduled callbacks.</summary>
        public int PendingCount => this.items.Count;

        /// <inheritdoc/>
        public IDisposable Schedule(TimeSpan dueTime, Action callback)
        {
            return this.ScheduleCore(dueTime, TimeSpan.Zero, callback);
        }

        /// <inheritdoc/>
        public IDisposable SchedulePeriodic(TimeSpan dueTime, TimeSpan period, Action callback)
        {
            if (period <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(period));
            }

            return this.ScheduleCore(dueTime, period, callback);
        }

        /// <summary>Advances the clock by a non-negative duration and runs all work that becomes due.</summary>
        /// <param name="duration">The duration to advance.</param>
        public void AdvanceBy(TimeSpan duration)
        {
            if (duration < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(duration));
            }

            this.AdvanceTo(this.now + duration);
        }

        /// <summary>Advances the clock to an absolute time and runs all work that becomes due.</summary>
        /// <param name="target">The target time, which must not precede <see cref="Now"/>.</param>
        public void AdvanceTo(TimeSpan target)
        {
            if (target < this.now)
            {
                throw new ArgumentOutOfRangeException(nameof(target));
            }

            List<Exception> errors = null;
            while (this.TryGetNext(target, out var item))
            {
                this.now = item.DueTime;
                var callback = item.Callback;
                if (item.Period == TimeSpan.Zero)
                {
                    this.items.Remove(item);
                    item.Detach();
                }
                else
                {
                    item.DueTime += item.Period;
                    item.Sequence = ++this.sequence;
                }

                try
                {
                    callback();
                }
                catch (Exception error)
                {
                    AddError(ref errors, error);
                }
            }

            this.now = target;
            if (errors != null)
            {
                throw new AggregateException("One or more scheduled callbacks failed.", errors);
            }
        }

        /// <summary>Cancels and releases all pending callbacks without changing the current time.</summary>
        public void Clear()
        {
            for (int index = 0; index < this.items.Count; ++index)
            {
                this.items[index].Detach();
            }

            this.items.Clear();
        }

        private static void AddError(ref List<Exception> errors, Exception error)
        {
            var aggregate = error as AggregateException;
            if (aggregate == null)
            {
                (errors ?? (errors = new List<Exception>())).Add(error);
                return;
            }

            if (errors == null)
            {
                errors = new List<Exception>(aggregate.InnerExceptions.Count);
            }

            errors.AddRange(aggregate.InnerExceptions);
        }

        private IDisposable ScheduleCore(TimeSpan dueTime, TimeSpan period, Action callback)
        {
            if (dueTime < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(dueTime));
            }

            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            var item = new ScheduledItem(
                this,
                callback,
                this.now + dueTime,
                period,
                ++this.sequence);
            this.items.Add(item);
            return item;
        }

        private bool TryGetNext(TimeSpan target, out ScheduledItem result)
        {
            result = null;
            for (int index = 0; index < this.items.Count; ++index)
            {
                var candidate = this.items[index];
                if (candidate.DueTime > target ||
                    (result != null && Compare(candidate, result) >= 0))
                {
                    continue;
                }

                result = candidate;
            }

            return result != null;
        }

        private static int Compare(ScheduledItem left, ScheduledItem right)
        {
            int dueTime = left.DueTime.CompareTo(right.DueTime);
            return dueTime != 0 ? dueTime : left.Sequence.CompareTo(right.Sequence);
        }

        private void Cancel(ScheduledItem item)
        {
            if (!ReferenceEquals(item.Owner, this))
            {
                return;
            }

            this.items.Remove(item);
            item.Detach();
        }

        private sealed class ScheduledItem : IDisposable
        {
            internal ScheduledItem(
                ManualTimerScheduler owner,
                Action callback,
                TimeSpan dueTime,
                TimeSpan period,
                long sequence)
            {
                this.Owner = owner;
                this.Callback = callback;
                this.DueTime = dueTime;
                this.Period = period;
                this.Sequence = sequence;
            }

            internal ManualTimerScheduler Owner { get; private set; }

            internal Action Callback { get; private set; }

            internal TimeSpan DueTime { get; set; }

            internal TimeSpan Period { get; }

            internal long Sequence { get; set; }

            public void Dispose()
            {
                this.Owner?.Cancel(this);
            }

            internal void Detach()
            {
                this.Owner = null;
                this.Callback = null;
            }
        }
    }
}
