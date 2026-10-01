// <copyright file="PropertyExtensions.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive
{
    using System;

    /// <summary>Provides subscription and lifetime helpers for reactive properties.</summary>
    public static class PropertyExtensions
    {
        /// <summary>Synchronously emits the current value, then subsequent changes.</summary>
        /// <typeparam name="T">The property value type.</typeparam>
        /// <param name="source">The property to observe.</param>
        /// <param name="onNext">The callback that receives each value.</param>
        /// <returns>A handle that unsubscribes the callback when disposed.</returns>
        public static IDisposable Subscribe<T>(this IReadOnlyReactiveProperty<T> source, Action<T> onNext)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (onNext == null)
            {
                throw new ArgumentNullException(nameof(onNext));
            }

            return new PropertySubscription<T>(source, onNext);
        }

        /// <summary>Synchronously emits the current value, then changes with the last delivered value.</summary>
        /// <typeparam name="T">The property value type.</typeparam>
        /// <param name="source">The property to observe.</param>
        /// <param name="onNext">The callback that receives initial and subsequent value notifications.</param>
        /// <returns>A handle that unsubscribes the callback when disposed.</returns>
        /// <remarks>
        /// The initial notification has <see cref="PropertyChange{T}.HasPreviousValue"/> set to <see langword="false"/>.
        /// Batched writes report the last delivered value and the final value of the batch.
        /// </remarks>
        public static IDisposable SubscribeChanges<T>(
            this IReadOnlyReactiveProperty<T> source,
            Action<PropertyChange<T>> onNext)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (onNext == null)
            {
                throw new ArgumentNullException(nameof(onNext));
            }

            return new PropertyChangeSubscription<T>(source, onNext);
        }

        /// <summary>Adds a disposable resource to a reactive scope.</summary>
        /// <typeparam name="T">The disposable resource type.</typeparam>
        /// <param name="disposable">The resource to own.</param>
        /// <param name="scope">The scope that will own the resource.</param>
        /// <returns>The supplied resource.</returns>
        public static T AddTo<T>(this T disposable, ReactiveScope scope)
            where T : IDisposable
        {
            if (scope == null)
            {
                throw new ArgumentNullException(nameof(scope));
            }

            return scope.Add(disposable);
        }

        /// <summary>Adds a disposable resource to a composite owner.</summary>
        /// <typeparam name="T">The disposable resource type.</typeparam>
        /// <param name="disposable">The resource to own.</param>
        /// <param name="owner">The composite that will own the resource.</param>
        /// <returns>The supplied resource.</returns>
        public static T AddTo<T>(this T disposable, CompositeDisposable owner)
            where T : IDisposable
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            return owner.Add(disposable);
        }

        // A property subscription has exactly one declared source; it needs no dynamic graph tracker.
        private sealed class PropertySubscription<T> : SignalListener
        {
            private readonly object valueSource;
            private readonly Action<T> onNext;
            private bool disposed;
            private bool running;

            internal PropertySubscription(IReadOnlyReactiveProperty<T> source, Action<T> onNext)
            {
                this.valueSource = source;
                this.onNext = onNext;
                source.OnChanged.Attach(this, false);
                try
                {
                    this.Run();
                }
                catch
                {
                    this.Dispose();
                    throw;
                }
            }

            public override void Dispose()
            {
                if (this.disposed)
                {
                    return;
                }

                this.disposed = true;
                base.Dispose();
            }

            internal override void OnSignal()
            {
                this.Run();
            }

            private void Run()
            {
                if (this.disposed)
                {
                    return;
                }

#if UNITY_5_3_OR_NEWER
                bool profiling = ReactiveProfiler.Enabled;
                if (profiling)
                {
                    ReactiveProfiler.PropertySubscriptionRun.Begin();
                }

                try
                {
#endif
                if (this.running)
                {
                    throw new InvalidOperationException("Reentrant property subscription detected.");
                }

                this.running = true;
                try
                {
                    if (this.valueSource is ReactiveProperty<T> property)
                    {
                        Tracking.CapturePropertyValue(property.Peek(), this.onNext);
                    }
                    else
                    {
                        Tracking.CaptureProperty((IReadOnlyReactiveProperty<T>)this.valueSource, this.onNext);
                    }
                }
                finally
                {
                    this.running = false;
                }

                if (Tracking.HasPendingPosts)
                {
                    Tracking.Post(null);
                }

#if UNITY_5_3_OR_NEWER
                }
                finally
                {
                    if (profiling)
                    {
                        ReactiveProfiler.PropertySubscriptionRun.End();
                    }
                }
#endif
            }
        }

        // This separate subscription keeps the ordinary Subscribe path free of previous-value storage and copies.
        private sealed class PropertyChangeSubscription<T> : SignalListener
        {
            private readonly object valueSource;
            private readonly Action<PropertyChange<T>> onNext;
            private T lastValue;
            private bool hasValue;
            private bool disposed;
            private bool running;

            internal PropertyChangeSubscription(
                IReadOnlyReactiveProperty<T> source,
                Action<PropertyChange<T>> onNext)
            {
                this.valueSource = source;
                this.onNext = onNext;
                source.OnChanged.Attach(this, false);
                try
                {
                    this.Run();
                }
                catch
                {
                    this.Dispose();
                    throw;
                }
            }

            public override void Dispose()
            {
                if (this.disposed)
                {
                    return;
                }

                this.disposed = true;
                base.Dispose();
            }

            internal override void OnSignal()
            {
                this.Run();
            }

            private void Run()
            {
                if (this.disposed)
                {
                    return;
                }

#if UNITY_5_3_OR_NEWER
                bool profiling = ReactiveProfiler.Enabled;
                if (profiling)
                {
                    ReactiveProfiler.PropertySubscriptionRun.Begin();
                }

                try
                {
#endif
                if (this.running)
                {
                    throw new InvalidOperationException("Reentrant property subscription detected.");
                }

                this.running = true;
                try
                {
                    T current;
                    if (this.valueSource is ReactiveProperty<T> property)
                    {
                        current = property.Peek();
                        Tracking.CapturePropertyValue(
                            new PropertyChange<T>(this.hasValue, this.lastValue, current),
                            this.onNext);
                    }
                    else
                    {
                        current = Tracking.CapturePropertyChange(
                            (IReadOnlyReactiveProperty<T>)this.valueSource,
                            this.hasValue,
                            this.lastValue,
                            this.onNext);
                    }

                    this.lastValue = current;
                    this.hasValue = true;
                }
                finally
                {
                    this.running = false;
                }

                if (Tracking.HasPendingPosts)
                {
                    Tracking.Post(null);
                }

#if UNITY_5_3_OR_NEWER
                }
                finally
                {
                    if (profiling)
                    {
                        ReactiveProfiler.PropertySubscriptionRun.End();
                    }
                }
#endif
            }
        }
    }
}
