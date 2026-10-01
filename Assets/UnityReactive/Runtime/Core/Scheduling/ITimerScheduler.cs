// <copyright file="ITimerScheduler.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive
{
    using System;

    /// <summary>Defines a replaceable clock and delayed-work scheduler for time-based event streams.</summary>
    /// <remarks>
    /// Implementations must not invoke a callback before the scheduling method returns. Disposing the returned
    /// handle cancels future invocations. The execution context and thread-safety rules are implementation-defined.
    /// </remarks>
    public interface ITimerScheduler
    {
        /// <summary>Gets the scheduler's monotonic elapsed time.</summary>
        TimeSpan Now { get; }

        /// <summary>Schedules one callback after the specified delay.</summary>
        /// <param name="dueTime">The non-negative delay before execution.</param>
        /// <param name="callback">The callback to execute.</param>
        /// <returns>A handle that cancels the pending callback.</returns>
        IDisposable Schedule(TimeSpan dueTime, Action callback);

        /// <summary>Schedules a callback periodically after an initial delay.</summary>
        /// <param name="dueTime">The non-negative delay before the first execution.</param>
        /// <param name="period">The positive interval between executions.</param>
        /// <param name="callback">The callback to execute.</param>
        /// <returns>A handle that cancels future callbacks.</returns>
        IDisposable SchedulePeriodic(TimeSpan dueTime, TimeSpan period, Action callback);
    }
}
