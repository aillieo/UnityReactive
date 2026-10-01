// <copyright file="RxUnity.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive.Unity
{
    using System;
    using System.Collections.Generic;

    /// <summary>Exposes Unity PlayerLoop events and Unity-time event streams.</summary>
    public static class RxUnity
    {
        private static EventSignal<Unit> everyUpdate = new EventSignal<Unit>();
        private static ManualTimerScheduler scaledTimeScheduler = new ManualTimerScheduler();
        private static ManualTimerScheduler unscaledTimeScheduler = new ManualTimerScheduler();

        /// <summary>Gets a shared hot stream that emits <see cref="Unit.Default"/> during each Unity Update phase.</summary>
        public static IEventStream<Unit> EveryUpdate
        {
            get
            {
                ReactiveFrameDriver.EnsureInstalled();
                return everyUpdate;
            }
        }

        /// <summary>Gets the timer scheduler advanced by <c>Time.deltaTime</c>.</summary>
        public static ITimerScheduler ScaledTimeScheduler
        {
            get
            {
                ReactiveFrameDriver.EnsureInstalled();
                return scaledTimeScheduler;
            }
        }

        /// <summary>Gets the timer scheduler advanced by <c>Time.unscaledDeltaTime</c>.</summary>
        public static ITimerScheduler UnscaledTimeScheduler
        {
            get
            {
                ReactiveFrameDriver.EnsureInstalled();
                return unscaledTimeScheduler;
            }
        }

        /// <summary>Creates a cold stream that emits zero once after a Unity-time delay.</summary>
        /// <param name="dueTime">The non-negative delay before emission.</param>
        /// <param name="timeMode">The Unity clock that advances the timer.</param>
        /// <returns>A stream that creates an independent timer for each subscriber.</returns>
        public static IEventStream<long> Timer(
            TimeSpan dueTime,
            ReactiveTimeMode timeMode = ReactiveTimeMode.Scaled)
        {
            return Rx.Timer(dueTime, GetScheduler(timeMode));
        }

        /// <summary>Creates a cold stream that emits increasing values periodically using Unity time.</summary>
        /// <param name="period">The positive delay before the first and each subsequent emission.</param>
        /// <param name="timeMode">The Unity clock that advances the interval.</param>
        /// <returns>A stream that creates an independent interval for each subscriber.</returns>
        public static IEventStream<long> Interval(
            TimeSpan period,
            ReactiveTimeMode timeMode = ReactiveTimeMode.Scaled)
        {
            return Rx.Interval(period, GetScheduler(timeMode));
        }

        /// <summary>Creates a cold periodic stream with a separate initial Unity-time delay.</summary>
        /// <param name="dueTime">The non-negative delay before the first emission.</param>
        /// <param name="period">The positive interval between emissions.</param>
        /// <param name="timeMode">The Unity clock that advances the interval.</param>
        /// <returns>A stream that creates an independent interval for each subscriber.</returns>
        public static IEventStream<long> Interval(
            TimeSpan dueTime,
            TimeSpan period,
            ReactiveTimeMode timeMode = ReactiveTimeMode.Scaled)
        {
            return Rx.Interval(dueTime, period, GetScheduler(timeMode));
        }

        internal static void AdvanceFrame(float deltaTime, float unscaledDeltaTime)
        {
            List<Exception> errors = null;
            AdvanceScheduler(
                scaledTimeScheduler,
                TimeSpan.FromSeconds(Math.Max(0, deltaTime)),
                ref errors);
            AdvanceScheduler(
                unscaledTimeScheduler,
                TimeSpan.FromSeconds(Math.Max(0, unscaledDeltaTime)),
                ref errors);

            try
            {
                everyUpdate.Emit(Unit.Default);
            }
            catch (Exception error)
            {
                AddError(ref errors, error);
            }

            if (errors != null)
            {
                throw new AggregateException("One or more Unity reactive callbacks failed.", errors);
            }
        }

        internal static void ResetStatics()
        {
            scaledTimeScheduler.Clear();
            unscaledTimeScheduler.Clear();
            everyUpdate = new EventSignal<Unit>();
            scaledTimeScheduler = new ManualTimerScheduler();
            unscaledTimeScheduler = new ManualTimerScheduler();
        }

        private static ITimerScheduler GetScheduler(ReactiveTimeMode timeMode)
        {
            ReactiveFrameDriver.EnsureInstalled();
            switch (timeMode)
            {
                case ReactiveTimeMode.Scaled:
                    return scaledTimeScheduler;
                case ReactiveTimeMode.Unscaled:
                    return unscaledTimeScheduler;
                default:
                    throw new ArgumentOutOfRangeException(nameof(timeMode));
            }
        }

        private static void AdvanceScheduler(
            ManualTimerScheduler scheduler,
            TimeSpan duration,
            ref List<Exception> errors)
        {
            try
            {
                scheduler.AdvanceBy(duration);
            }
            catch (Exception error)
            {
                AddError(ref errors, error);
            }
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
    }
}
