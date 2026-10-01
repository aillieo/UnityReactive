// <copyright file="Rx.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive
{
    using System;
    using System.Collections.Generic;

    /// <summary>All APIs are synchronous and confined to the Unity main thread (or one owning thread).</summary>
    public static class Rx
    {
        /// <summary>Gets the scheduler used by overloads that do not specify one.</summary>
        public static DeferredScheduler FrameScheduler { get; } = new DeferredScheduler();

        /// <summary>Creates a mutable reactive property.</summary>
        /// <typeparam name="T">The property value type.</typeparam>
        /// <param name="value">The initial value.</param>
        /// <param name="comparer">The comparer used to suppress equal-value notifications.</param>
        /// <returns>A new mutable reactive property.</returns>
        public static ReactiveProperty<T> Property<T>(T value, IEqualityComparer<T> comparer = null)
        {
            return new ReactiveProperty<T>(value, comparer);
        }

        /// <summary>Creates a lossless synchronous event stream.</summary>
        /// <typeparam name="T">The event payload type.</typeparam>
        /// <returns>A new event stream.</returns>
        public static EventSignal<T> Event<T>()
        {
            return new EventSignal<T>();
        }

        /// <summary>Creates a cold event stream that emits zero once after a delay.</summary>
        /// <param name="dueTime">The non-negative delay before emission.</param>
        /// <param name="scheduler">The scheduler that provides time and executes the emission.</param>
        /// <returns>A stream that creates an independent timer for each subscriber.</returns>
        public static IEventStream<long> Timer(TimeSpan dueTime, ITimerScheduler scheduler)
        {
            if (dueTime < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(dueTime));
            }

            return new TimerEventStream(
                scheduler ?? throw new ArgumentNullException(nameof(scheduler)),
                dueTime,
                TimeSpan.Zero);
        }

        /// <summary>Creates a cold event stream that emits increasing values periodically.</summary>
        /// <param name="period">The positive delay before the first and each subsequent emission.</param>
        /// <param name="scheduler">The scheduler that provides time and executes emissions.</param>
        /// <returns>A stream that creates an independent interval for each subscriber.</returns>
        public static IEventStream<long> Interval(TimeSpan period, ITimerScheduler scheduler)
        {
            return Interval(period, period, scheduler);
        }

        /// <summary>Creates a cold event stream that emits increasing values periodically after an initial delay.</summary>
        /// <param name="dueTime">The non-negative delay before the first emission.</param>
        /// <param name="period">The positive interval between emissions.</param>
        /// <param name="scheduler">The scheduler that provides time and executes emissions.</param>
        /// <returns>A stream that creates an independent interval for each subscriber.</returns>
        public static IEventStream<long> Interval(TimeSpan dueTime, TimeSpan period, ITimerScheduler scheduler)
        {
            if (dueTime < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(dueTime));
            }

            if (period <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(period));
            }

            return new TimerEventStream(
                scheduler ?? throw new ArgumentNullException(nameof(scheduler)),
                dueTime,
                period);
        }

        /// <summary>Creates a reactive property backed by custom getter and setter delegates.</summary>
        /// <typeparam name="T">The property value type.</typeparam>
        /// <param name="getter">The delegate that reads the current value.</param>
        /// <param name="setter">The delegate that writes a value and returns whether observers should be notified.</param>
        /// <returns>A new custom reactive property.</returns>
        public static CustomReactiveProperty<T> Custom<T>(Func<T> getter, Func<T, bool> setter)
        {
            return new CustomReactiveProperty<T>(getter, setter);
        }

        /// <summary>Creates an observable collection.</summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <returns>A new empty reactive collection.</returns>
        public static ReactiveCollection<T> Collection<T>()
        {
            return new ReactiveCollection<T>();
        }

        /// <summary>Creates an observable collection with the specified per-index signal cache policy.</summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="keySignalCachePolicy">The policy used to retain per-index signals.</param>
        /// <returns>A new empty reactive collection.</returns>
        public static ReactiveCollection<T> Collection<T>(KeySignalCachePolicy keySignalCachePolicy)
        {
            return new ReactiveCollection<T>(keySignalCachePolicy);
        }

        /// <summary>Creates an observable dictionary.</summary>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <returns>A new empty reactive dictionary.</returns>
        public static ReactiveDictionary<TKey, TValue> Dictionary<TKey, TValue>()
        {
            return new ReactiveDictionary<TKey, TValue>();
        }

        /// <summary>Creates an observable dictionary with the specified per-key signal cache policy.</summary>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <param name="keySignalCachePolicy">The policy used to retain per-key signals.</param>
        /// <returns>A new empty reactive dictionary.</returns>
        public static ReactiveDictionary<TKey, TValue> Dictionary<TKey, TValue>(KeySignalCachePolicy keySignalCachePolicy)
        {
            return new ReactiveDictionary<TKey, TValue>(keySignalCachePolicy);
        }

        /// <summary>Creates an observable set.</summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <returns>A new empty reactive set.</returns>
        public static ReactiveSet<T> Set<T>()
        {
            return new ReactiveSet<T>();
        }

        /// <summary>Creates an observable set with the specified per-item signal cache policy.</summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="keySignalCachePolicy">The policy used to retain per-item signals.</param>
        /// <returns>A new empty reactive set.</returns>
        public static ReactiveSet<T> Set<T>(KeySignalCachePolicy keySignalCachePolicy)
        {
            return new ReactiveSet<T>(keySignalCachePolicy);
        }

        /// <summary>Creates a computed property whose dependencies are discovered while evaluating the reducer.</summary>
        /// <typeparam name="T">The computed value type.</typeparam>
        /// <param name="reducer">The function that computes the current value.</param>
        /// <returns>A new computed reactive property.</returns>
        public static ComputedReactiveProperty<T> Computed<T>(Func<T> reducer)
        {
            return new ComputedReactiveProperty<T>(reducer);
        }

        /// <summary>Creates a computed property with explicitly supplied dependencies.</summary>
        /// <typeparam name="T">The computed value type.</typeparam>
        /// <param name="reducer">The function that computes the current value.</param>
        /// <param name="dependencies">The sources that invalidate the computed value.</param>
        /// <returns>A new computed reactive property.</returns>
        public static ComputedReactiveProperty<T> Computed<T>(Func<T> reducer, params IReactiveSource[] dependencies)
        {
            return new ComputedReactiveProperty<T>(reducer, dependencies);
        }

        /// <summary>Observes an effect using the default frame scheduler.</summary>
        /// <param name="effect">The effect to run and re-run when its tracked dependencies change.</param>
        /// <returns>The observation lifetime.</returns>
        public static ReactiveSubscription Observe(Action effect)
        {
            return Observe(effect, FrameScheduler);
        }

        /// <summary>Observes an effect using the specified scheduler.</summary>
        /// <param name="effect">The effect to run and re-run when its tracked dependencies change.</param>
        /// <param name="scheduler">The scheduler used for subsequent executions, or <see langword="null"/> for synchronous dispatch.</param>
        /// <returns>The observation lifetime.</returns>
        public static ReactiveSubscription Observe(Action effect, IReactiveScheduler scheduler)
        {
            return new ReactiveSubscription(effect, null, scheduler);
        }

        /// <summary>Observes an effect using one explicit dependency and the default frame scheduler.</summary>
        /// <param name="effect">The effect to run.</param>
        /// <param name="dependency">The source that invalidates the effect.</param>
        /// <returns>The observation lifetime.</returns>
        public static ReactiveSubscription Observe(Action effect, IReactiveSource dependency)
        {
            return Observe(effect, new[] { dependency }, FrameScheduler);
        }

        /// <summary>Observes an effect using one explicit dependency and scheduler.</summary>
        /// <param name="effect">The effect to run.</param>
        /// <param name="dependency">The source that invalidates the effect.</param>
        /// <param name="scheduler">The scheduler used for subsequent executions.</param>
        /// <returns>The observation lifetime.</returns>
        public static ReactiveSubscription Observe(Action effect, IReactiveSource dependency, IReactiveScheduler scheduler)
        {
            return Observe(effect, new[] { dependency }, scheduler);
        }

        /// <summary>Observes an effect using explicit dependencies and the default frame scheduler.</summary>
        /// <param name="effect">The effect to run.</param>
        /// <param name="dependencies">The sources that invalidate the effect.</param>
        /// <returns>The observation lifetime.</returns>
        public static ReactiveSubscription Observe(Action effect, IReactiveSource[] dependencies)
        {
            return Observe(effect, dependencies, FrameScheduler);
        }

        /// <summary>Observes an effect using explicit dependencies and scheduler.</summary>
        /// <param name="effect">The effect to run.</param>
        /// <param name="dependencies">The sources that invalidate the effect.</param>
        /// <param name="scheduler">The scheduler used for subsequent executions.</param>
        /// <returns>The observation lifetime.</returns>
        public static ReactiveSubscription Observe(Action effect, IReactiveSource[] dependencies, IReactiveScheduler scheduler)
        {
            return new ReactiveSubscription(effect, dependencies ?? throw new ArgumentNullException(nameof(dependencies)), scheduler);
        }

        /// <summary>Observes a value-producing effect and invokes a post-effect after each execution.</summary>
        /// <typeparam name="T">The produced value type.</typeparam>
        /// <param name="effect">The function to evaluate reactively.</param>
        /// <param name="postEffect">The callback that consumes the produced value outside dependency tracking.</param>
        /// <returns>The observation lifetime.</returns>
        public static ReactiveSubscription Observe<T>(Func<T> effect, Action<T> postEffect)
        {
            return Observe(effect, postEffect, FrameScheduler);
        }

        /// <summary>Observes a value-producing effect with a specified scheduler.</summary>
        /// <typeparam name="T">The produced value type.</typeparam>
        /// <param name="effect">The function to evaluate reactively.</param>
        /// <param name="postEffect">The callback that consumes the produced value outside dependency tracking.</param>
        /// <param name="scheduler">The scheduler used for subsequent executions.</param>
        /// <returns>The observation lifetime.</returns>
        public static ReactiveSubscription Observe<T>(Func<T> effect, Action<T> postEffect, IReactiveScheduler scheduler)
        {
            if (effect == null)
            {
                throw new ArgumentNullException(nameof(effect));
            }

            if (postEffect == null)
            {
                throw new ArgumentNullException(nameof(postEffect));
            }

            T result = default;
            return new ReactiveSubscription(() => result = effect(), null, scheduler, () => postEffect(result));
        }

        /// <summary>Begins a batch scope; notifications flush when the returned handle is disposed.</summary>
        /// <returns>A scope handle that completes the batch when disposed.</returns>
        public static IDisposable Batch()
        {
            ++Dispatch.Depth;
            return Disposable.Create(() =>
            {
                --Dispatch.Depth;
                Dispatch.Flush();
            });
        }

        /// <summary>Executes an action as one notification batch.</summary>
        /// <param name="action">The action to execute.</param>
        public static void Batch(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            ++Dispatch.Depth;
            try
            {
                action();
            }
            finally
            {
                --Dispatch.Depth;
                Dispatch.Flush();
            }
        }

        /// <summary>Executes an action with notifications delayed until it completes.</summary>
        /// <param name="action">The action to execute.</param>
        public static void DoDelayNotify(Action action)
        {
            Batch(action);
        }

        /// <summary>Temporarily disables dependency tracking until the returned handle is disposed.</summary>
        /// <returns>A scope handle that restores the previous tracking context.</returns>
        /// <remarks>
        /// Pause scopes are synchronous and must be disposed in reverse creation order in the context that created them.
        /// Prefer <see cref="Untracked(Action)"/> when a disposable scope is not required.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The handle is disposed out of order or outside its creating context.</exception>
        public static IDisposable PauseTracking()
        {
            return Tracking.Pause();
        }

        /// <summary>Runs an action without recording reactive dependencies.</summary>
        /// <param name="action">The action to execute.</param>
        public static void Untracked(Action action)
        {
            Tracking.Untracked(action);
        }

        /// <summary>Evaluates a function without recording reactive dependencies.</summary>
        /// <typeparam name="T">The returned value type.</typeparam>
        /// <param name="action">The function to evaluate.</param>
        /// <returns>The function result.</returns>
        public static T Untracked<T>(Func<T> action)
        {
            return Tracking.Untracked(action);
        }
    }
}
