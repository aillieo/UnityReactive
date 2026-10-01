// <copyright file="ReactiveScope.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive
{
    using System;

    /// <summary>Creates observations with one default scheduler and owns their lifetimes.</summary>
    public sealed class ReactiveScope : IDisposable
    {
        private readonly CompositeDisposable disposables = new CompositeDisposable();
        private readonly IReactiveScheduler scheduler;

        /// <summary>Initializes a new instance of the <see cref="ReactiveScope"/> class using <see cref="Rx.FrameScheduler"/>.</summary>
        public ReactiveScope()
            : this(Rx.FrameScheduler)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="ReactiveScope"/> class.</summary>
        /// <param name="scheduler">The default scheduler for observations created by the scope.</param>
        public ReactiveScope(IReactiveScheduler scheduler)
        {
            this.scheduler = scheduler;
        }

        /// <summary>Gets the number of resources owned by the scope.</summary>
        public int Count => this.disposables.Count;

        /// <summary>Gets a value indicating whether the scope has been disposed.</summary>
        public bool IsDisposed => this.disposables.IsDisposed;

        /// <summary>Adds a disposable resource to the scope.</summary>
        /// <typeparam name="T">The disposable resource type.</typeparam>
        /// <param name="handle">The resource to own.</param>
        /// <returns>The supplied resource.</returns>
        public T Add<T>(T handle)
            where T : IDisposable
        {
            return this.disposables.Add(handle);
        }

        /// <summary>Observes an automatically tracked effect using the scope's default scheduler.</summary>
        /// <param name="effect">The effect to observe.</param>
        /// <returns>The owned observation.</returns>
        public ReactiveSubscription Observe(Action effect)
        {
            this.ThrowIfDisposed();
            return this.Add(Rx.Observe(effect, this.scheduler));
        }

        /// <summary>Observes an automatically tracked effect using a specified scheduler.</summary>
        /// <param name="effect">The effect to observe.</param>
        /// <param name="scheduler">The scheduler used for subsequent executions.</param>
        /// <returns>The owned observation.</returns>
        public ReactiveSubscription Observe(Action effect, IReactiveScheduler scheduler)
        {
            this.ThrowIfDisposed();
            return this.Add(Rx.Observe(effect, scheduler));
        }

        /// <summary>Observes an effect with explicit dependencies using the default scheduler.</summary>
        /// <param name="effect">The effect to observe.</param>
        /// <param name="dependencies">The sources that invalidate the effect.</param>
        /// <returns>The owned observation.</returns>
        public ReactiveSubscription Observe(Action effect, params IReactiveSource[] dependencies)
        {
            this.ThrowIfDisposed();
            return this.Add(Rx.Observe(effect, dependencies, this.scheduler));
        }

        /// <summary>Observes an effect with explicit dependencies and scheduler.</summary>
        /// <param name="effect">The effect to observe.</param>
        /// <param name="dependencies">The sources that invalidate the effect.</param>
        /// <param name="scheduler">The scheduler used for subsequent executions.</param>
        /// <returns>The owned observation.</returns>
        public ReactiveSubscription Observe(Action effect, IReactiveSource[] dependencies, IReactiveScheduler scheduler)
        {
            this.ThrowIfDisposed();
            return this.Add(Rx.Observe(effect, dependencies, scheduler));
        }

        /// <summary>Observes a value-producing effect and owns its subscription.</summary>
        /// <typeparam name="T">The produced value type.</typeparam>
        /// <param name="effect">The function to evaluate reactively.</param>
        /// <param name="postEffect">The callback that consumes the result outside dependency tracking.</param>
        /// <returns>The owned observation.</returns>
        public ReactiveSubscription Observe<T>(Func<T> effect, Action<T> postEffect)
        {
            this.ThrowIfDisposed();
            return this.Add(Rx.Observe(effect, postEffect, this.scheduler));
        }

        /// <summary>Observes a value-producing effect using a specified scheduler.</summary>
        /// <typeparam name="T">The produced value type.</typeparam>
        /// <param name="effect">The function to evaluate reactively.</param>
        /// <param name="postEffect">The callback that consumes the result outside dependency tracking.</param>
        /// <param name="scheduler">The scheduler used for subsequent executions.</param>
        /// <returns>The owned observation.</returns>
        public ReactiveSubscription Observe<T>(Func<T> effect, Action<T> postEffect, IReactiveScheduler scheduler)
        {
            this.ThrowIfDisposed();
            return this.Add(Rx.Observe(effect, postEffect, scheduler));
        }

        /// <summary>Subscribes to a signal and owns the returned subscription.</summary>
        /// <param name="signal">The signal to observe.</param>
        /// <param name="action">The callback to invoke.</param>
        /// <param name="executeWhenBound">Whether to invoke the callback immediately after binding.</param>
        /// <returns>The owned signal subscription.</returns>
        public IDisposable ListenSignal(Signal signal, Action action, bool executeWhenBound = false)
        {
            this.ThrowIfDisposed();
            var handle = this.Add(signal.Subscribe(action));
            try
            {
                if (executeWhenBound)
                {
                    Rx.Untracked(action);
                }
            }
            catch
            {
                this.disposables.Remove(handle);
                throw;
            }

            return handle;
        }

        /// <summary>Creates an observation without adding it to this scope.</summary>
        /// <param name="effect">The effect to observe.</param>
        /// <param name="dependency">The source that invalidates the effect.</param>
        /// <returns>An unowned observation using this scope's default scheduler.</returns>
        internal ReactiveSubscription CreateObservation(Action effect, IReactiveSource dependency)
        {
            this.ThrowIfDisposed();
            return Rx.Observe(effect, dependency, this.scheduler);
        }

        /// <summary>Disposes current resources while keeping the scope reusable.</summary>
        public void Clear()
        {
            this.disposables.Clear();
        }

        /// <summary>Permanently disposes the scope and all owned resources.</summary>
        public void Dispose()
        {
            this.disposables.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (this.disposables.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(ReactiveScope));
            }
        }
    }
}
