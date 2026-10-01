// <copyright file="ComputedReactiveProperty.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive
{
    using System;
    using System.Collections.Generic;

    /// <summary>Represents a reactive value computed from tracked dependencies.</summary>
    /// <typeparam name="T">The computed value type.</typeparam>
    public sealed class ComputedReactiveProperty<T> : IReadOnlyReactiveProperty<T>, IDisposable
    {
        private readonly Func<T> reducer;
        private readonly IEqualityComparer<T> comparer;
        private readonly bool automatic;
        private readonly DependencyTracker dependencies;
        private readonly RefreshNode refreshNode;
        private T value;
        private bool dirty = true;
        private bool initialized;
        private bool evaluating;
        private bool disposed;
        private bool checking;

        /// <summary>Initializes a new instance of the <see cref="ComputedReactiveProperty{T}"/> class.</summary>
        /// <param name="reducer">The function that computes the current value.</param>
        /// <param name="dependencies">Explicit dependencies, or <see langword="null"/> to discover dependencies automatically.</param>
        /// <param name="comparer">The comparer used to suppress equal-value notifications.</param>
        public ComputedReactiveProperty(Func<T> reducer, IReactiveSource[] dependencies = null, IEqualityComparer<T> comparer = null)
        {
            this.reducer = reducer ?? throw new ArgumentNullException(nameof(reducer));
            this.comparer = comparer ?? EqualityComparer<T>.Default;
            this.automatic = dependencies == null;
            this.dependencies = new DependencyTracker(this.Invalidate);
            this.refreshNode = new RefreshNode(this);
            this.OnChanged.EnsureCurrent = this.EnsureCurrent;
            if (!this.automatic)
            {
                foreach (var dependency in dependencies)
                {
                    if (dependency == null)
                    {
                        throw new ArgumentException("Null dependency.", nameof(dependencies));
                    }
                }

                var unique = new HashSet<Signal>();
                foreach (var dependency in dependencies)
                {
                    if (unique.Add(dependency.OnChanged))
                    {
                        this.dependencies.Add(dependency.OnChanged);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public T Value
        {
            get
            {
                if (this.disposed)
                {
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                this.OnChanged.Track();
                if (this.dirty || this.dependencies.RequiresCurrentCheck)
                {
                    this.EnsureCurrent();
                }

                if (Tracking.HasPendingPosts)
                {
                    Tracking.Post(null);
                }

                return this.value;
            }
        }

        /// <inheritdoc/>
        Signal IReactiveSource.OnChanged => this.OnChanged;

        /// <summary>Gets the signal raised when the computed value changes.</summary>
        internal Signal OnChanged { get; } = new Signal();

        /// <summary>Stops dependency tracking and releases all dependency subscriptions.</summary>
        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.dependencies.Dispose();
        }

        private void Invalidate()
        {
            if (this.disposed)
            {
                return;
            }

            this.dirty = true;
            if (this.OnChanged.ListenerCount != 0 && !this.refreshNode.Queued)
            {
                Dispatch.Enqueue(this.refreshNode, true);
            }
        }

        private void Refresh()
        {
            if (!this.disposed)
            {
                this.EnsureCurrent();
            }
        }

        private void EnsureCurrent()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            if (this.checking)
            {
                throw new InvalidOperationException("Circular computed dependency detected.");
            }

            this.checking = true;
            try
            {
                if (this.dependencies.RequiresCurrentCheck)
                {
                    this.dependencies.EnsureCurrent();
                }

                if (this.dirty)
                {
                    this.Evaluate();
                }
            }
            finally
            {
                this.checking = false;
            }
        }

        private void Evaluate()
        {
            if (this.evaluating)
            {
                throw new InvalidOperationException("Circular computed dependency detected.");
            }

#if UNITY_5_3_OR_NEWER
            bool profiling = ReactiveProfiler.Enabled;
            if (profiling)
            {
                ReactiveProfiler.ComputedEvaluate.Begin();
            }

            try
            {
#endif
            this.evaluating = true;
            var reads = this.automatic ? this.dependencies.Reads : null;
            if (this.automatic)
            {
                this.dependencies.BeginCapture();
            }

            T next;
            try
            {
                next = Tracking.Capture(this.reducer, reads);
            }
            finally
            {
                this.evaluating = false;

                // Keep reads made before an exception so a later input change can recover.
                if (this.automatic && !this.disposed)
                {
                    bool dependenciesUnchanged = this.dependencies.EndCapture();
                    reads.Remove(this.OnChanged);
                    if (!dependenciesUnchanged)
                    {
                        this.dependencies.Synchronize();
                    }
                }
                else if (this.disposed)
                {
                    reads?.Clear();
                }
            }

            if (this.disposed)
            {
                return;
            }

            bool changed = this.initialized && !this.comparer.Equals(this.value, next);
            this.value = next;
            this.initialized = true;
            this.dirty = false;
            if (changed)
            {
                this.OnChanged.Notify();
            }
#if UNITY_5_3_OR_NEWER
            }
            finally
            {
                if (profiling)
                {
                    ReactiveProfiler.ComputedEvaluate.End();
                }
            }
#endif
        }

        private sealed class RefreshNode : DispatchNode
        {
            private readonly ComputedReactiveProperty<T> owner;

            internal RefreshNode(ComputedReactiveProperty<T> owner)
            {
                this.owner = owner;
            }

            internal override void Invoke()
            {
                this.owner.Refresh();
            }
        }
    }
}
