// <copyright file="ReactiveSubscription.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive
{
    using System;
    using System.Collections.Generic;

    /// <summary>Runs a reactive effect and schedules it when dependencies change.</summary>
    public sealed class ReactiveSubscription : IDisposable
    {
        private readonly Action effect;
        private readonly Action postEffect;
        private readonly IReactiveScheduler scheduler;
        private readonly bool automatic;
        private readonly DependencyTracker dependencies;
        private readonly RunNode runNode;
        private readonly Action runAction;
        private readonly Action postAction;
        private bool disposed;
        private bool running;

        /// <summary>Initializes a new instance of the <see cref="ReactiveSubscription"/> class.</summary>
        /// <param name="effect">The reactive effect.</param>
        /// <param name="dependencies">Explicit dependencies, or <see langword="null"/> for automatic tracking.</param>
        /// <param name="scheduler">The scheduler for subsequent executions, or <see langword="null"/> for synchronous dispatch.</param>
        /// <param name="postEffect">An optional callback invoked after the effect outside dependency tracking.</param>
        internal ReactiveSubscription(Action effect, IReactiveSource[] dependencies, IReactiveScheduler scheduler, Action postEffect = null)
        {
            this.effect = effect ?? throw new ArgumentNullException(nameof(effect));
            this.scheduler = scheduler;
            this.postEffect = postEffect;
            this.automatic = dependencies == null;
            if (scheduler == null)
            {
                this.runNode = new RunNode(this);
            }
            else
            {
                this.runAction = this.Run;
            }

            this.postAction = this.InvokePost;
            this.dependencies = new DependencyTracker(this.Trigger);
            try
            {
                if (!this.automatic)
                {
                    var unique = new HashSet<Signal>();
                    foreach (var dependency in dependencies)
                    {
                        if (dependency == null)
                        {
                            throw new ArgumentException("Null dependency.", nameof(dependencies));
                        }

                        if (unique.Add(dependency.OnChanged))
                        {
                            this.dependencies.Add(dependency.OnChanged);
                        }
                    }
                }

                this.Run();
            }
            catch
            {
                this.Dispose();
                throw;
            }
        }

        /// <summary>Cancels pending execution and releases all dependency subscriptions.</summary>
        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            try
            {
                if (this.scheduler != null)
                {
                    this.scheduler.Cancel(this.runAction);
                }
            }
            finally
            {
                this.dependencies.Dispose();
            }
        }

        private void Trigger()
        {
            if (this.disposed)
            {
                return;
            }

            if (this.scheduler == null)
            {
                Dispatch.Enqueue(this.runNode, false);
            }
            else
            {
                this.scheduler.Schedule(this.runAction);
            }
        }

        private void Run()
        {
            if (this.disposed)
            {
                return;
            }

            if (this.running)
            {
                throw new InvalidOperationException("Reentrant reactive watch detected.");
            }

            this.running = true;
            var reads = this.automatic ? this.dependencies.Reads : null;
            if (this.automatic)
            {
                this.dependencies.BeginCapture();
            }

            try
            {
                Tracking.Capture(this.effect, reads);
            }
            finally
            {
                this.running = false;
                if (this.automatic && !this.disposed)
                {
                    if (!this.dependencies.EndCapture())
                    {
                        this.dependencies.Synchronize();
                    }
                }
                else if (this.disposed)
                {
                    reads?.Clear();
                }
            }

            Tracking.Post(!this.disposed && this.postEffect != null ? this.postAction : null);
        }

        private void InvokePost()
        {
            if (!this.disposed)
            {
                this.postEffect();
            }
        }

        private sealed class RunNode : DispatchNode
        {
            private readonly ReactiveSubscription owner;

            internal RunNode(ReactiveSubscription owner)
            {
                this.owner = owner;
            }

            internal override void Invoke()
            {
                this.owner.Run();
            }
        }
    }
}
