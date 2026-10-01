// <copyright file="Signal.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>Deterministic, single-threaded signal. Subscriptions must be disposed by their owner.</summary>
    public sealed class Signal : IReactiveSource
    {
        internal Action EnsureCurrent;
        private readonly NormalDispatchNode normalDispatchNode;
        private SignalListener priorityFirst;
        private SignalListener priorityLast;
        private SignalListener normalFirst;
        private SignalListener normalLast;
        private int count;
        private int normalCount;
        private int traversalDepth;
        private bool needsCleanup;
        private long notificationVersion;

        /// <summary>Initializes a new instance of the <see cref="Signal"/> class.</summary>
        public Signal()
        {
            this.normalDispatchNode = new NormalDispatchNode(this);
        }

        /// <summary>Gets the number of active listeners.</summary>
        public int ListenerCount => this.count;

        /// <inheritdoc/>
        Signal IReactiveSource.OnChanged => this;

        /// <summary>Subscribes a callback to this signal.</summary>
        /// <param name="callback">The callback invoked when the signal is notified.</param>
        /// <returns>The subscription lifetime.</returns>
        public IDisposable Subscribe(Action callback)
        {
            return this.Subscribe(callback, false);
        }

        /// <summary>Records this signal as a dependency when tracking is active.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Track()
        {
            Tracking.Current?.Add(this);
        }

        /// <summary>Notifies all current listeners that the source has changed.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Notify()
        {
            if (this.count == 0)
            {
                return;
            }

            if (this.priorityFirst == null &&
                this.traversalDepth == 0 &&
                !this.normalDispatchNode.Queued &&
                Tracking.Current == null &&
                Dispatch.IsIdle)
            {
                this.NotifyDirect();
            }
            else
            {
                this.NotifyCore();
            }
        }

        internal IDisposable Subscribe(Action callback, bool priority)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            var listener = new ActionListener(callback);
            this.Attach(listener, priority);
            return listener;
        }

        internal void Attach(SignalListener listener, bool priority)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            if (listener.Owner != null)
            {
                throw new InvalidOperationException("Signal listener is already attached.");
            }

            listener.Owner = this;
            listener.Priority = priority;
            listener.DeliveredVersion = this.notificationVersion;
            if (priority)
            {
                listener.Previous = this.priorityLast;
                if (this.priorityLast == null)
                {
                    this.priorityFirst = listener;
                }
                else
                {
                    this.priorityLast.Next = listener;
                }

                this.priorityLast = listener;
            }
            else
            {
                listener.Previous = this.normalLast;
                if (this.normalLast == null)
                {
                    this.normalFirst = listener;
                }
                else
                {
                    this.normalLast.Next = listener;
                }

                this.normalLast = listener;
                ++this.normalCount;
            }

            ++this.count;
        }

        internal void Detach(SignalListener listener)
        {
            if (!ReferenceEquals(listener.Owner, this))
            {
                return;
            }

            listener.Owner = null;
            listener.OnDetached();
            --this.count;
            if (!listener.Priority)
            {
                --this.normalCount;
            }

            if (this.traversalDepth == 0)
            {
                this.Unlink(listener);
            }
            else
            {
                this.needsCleanup = true;
            }
        }

        private void NotifyDirect()
        {
#if UNITY_5_3_OR_NEWER
            bool profiling = ReactiveProfiler.Enabled;
            if (profiling)
            {
                ReactiveProfiler.SignalNotify.Begin();
                ReactiveProfiler.SignalDispatchNormal.Begin();
            }

            try
            {
#endif
            unchecked
            {
                ++this.notificationVersion;
            }

            List<Exception> errors = null;
            ++Dispatch.Depth;
            ++this.traversalDepth;
            var boundary = this.normalLast;
            try
            {
                for (var listener = this.normalFirst; listener != null; listener = listener.Next)
                {
                    if (listener.Owner != null)
                    {
                        listener.DeliveredVersion = this.notificationVersion;
                        try
                        {
                            listener.OnSignal();
                        }
                        catch (Exception e)
                        {
                            (errors ?? (errors = new List<Exception>())).Add(e);
                        }

                        if (Dispatch.HasPriority)
                        {
                            Dispatch.Enqueue(this.normalDispatchNode, false);
                            break;
                        }
                    }

                    if (listener == boundary)
                    {
                        break;
                    }
                }
            }
            finally
            {
                this.EndTraversal();
                --Dispatch.Depth;
            }

            try
            {
                Dispatch.Flush();
            }
            catch (AggregateException e) when (errors != null)
            {
                errors.AddRange(e.InnerExceptions);
            }

            if (errors != null)
            {
                throw new AggregateException("Reactive notifications failed.", errors);
            }
#if UNITY_5_3_OR_NEWER
            }
            finally
            {
                if (profiling)
                {
                    ReactiveProfiler.SignalDispatchNormal.End();
                    ReactiveProfiler.SignalNotify.End();
                }
            }
#endif
        }

        private void NotifyCore()
        {
#if UNITY_5_3_OR_NEWER
            bool profiling = ReactiveProfiler.Enabled;
            if (profiling)
            {
                ReactiveProfiler.SignalNotify.Begin();
            }

            try
            {
#endif
            ++Dispatch.Depth;
            unchecked
            {
                ++this.notificationVersion;
            }

            if (this.normalCount != 0)
            {
                Dispatch.Enqueue(this.normalDispatchNode, false);
            }

            try
            {
                if (this.priorityFirst != null)
                {
                    this.DispatchPriorityListeners();
                }
            }
            finally
            {
                --Dispatch.Depth;
                Dispatch.Flush();
            }
#if UNITY_5_3_OR_NEWER
            }
            finally
            {
                if (profiling)
                {
                    ReactiveProfiler.SignalNotify.End();
                }
            }
#endif
        }

        private void DispatchPriorityListeners()
        {
            ++this.traversalDepth;

            // Priority listeners invalidate dependencies before normal observers run.
            // A captured tail excludes subscriptions added during this notification.
            var boundary = this.priorityLast;
            try
            {
                for (var listener = this.priorityFirst; listener != null; listener = listener.Next)
                {
                    if (listener.Owner != null)
                    {
                        listener.OnSignal();
                    }

                    if (listener == boundary)
                    {
                        break;
                    }
                }
            }
            finally
            {
                this.EndTraversal();
            }
        }

        private void DispatchNormalListeners()
        {
#if UNITY_5_3_OR_NEWER
            bool profiling = ReactiveProfiler.Enabled;
            if (profiling)
            {
                ReactiveProfiler.SignalDispatchNormal.Begin();
            }

            try
            {
#endif
            ++this.traversalDepth;
            var boundary = this.normalLast;
            try
            {
                for (var listener = this.normalFirst; listener != null; listener = listener.Next)
                {
                    if (listener.Owner != null && listener.DeliveredVersion != this.notificationVersion)
                    {
                        listener.DeliveredVersion = this.notificationVersion;
                        try
                        {
                            listener.OnSignal();
                        }
                        catch (Exception e)
                        {
                            Dispatch.Report(e);
                        }

                        // Preserve priority ordering when a normal observer invalidates
                        // a computed value needed by later observers.
                        if (Dispatch.HasPriority)
                        {
                            Dispatch.Enqueue(this.normalDispatchNode, false);
                            break;
                        }
                    }

                    if (listener == boundary)
                    {
                        break;
                    }
                }
            }
            finally
            {
                this.EndTraversal();
            }
#if UNITY_5_3_OR_NEWER
            }
            finally
            {
                if (profiling)
                {
                    ReactiveProfiler.SignalDispatchNormal.End();
                }
            }
#endif
        }

        private void EndTraversal()
        {
            if (--this.traversalDepth != 0 || !this.needsCleanup)
            {
                return;
            }

            this.needsCleanup = false;
            this.RemoveDetached(ref this.priorityFirst, ref this.priorityLast);
            this.RemoveDetached(ref this.normalFirst, ref this.normalLast);
        }

        private void RemoveDetached(ref SignalListener first, ref SignalListener last)
        {
            var listener = first;
            while (listener != null)
            {
                var next = listener.Next;
                if (listener.Owner == null)
                {
                    this.Unlink(listener);
                }

                listener = next;
            }
        }

        private void Unlink(SignalListener listener)
        {
            ref SignalListener first = ref (listener.Priority ? ref this.priorityFirst : ref this.normalFirst);
            ref SignalListener last = ref (listener.Priority ? ref this.priorityLast : ref this.normalLast);
            if (listener.Previous == null)
            {
                first = listener.Next;
            }
            else
            {
                listener.Previous.Next = listener.Next;
            }

            if (listener.Next == null)
            {
                last = listener.Previous;
            }
            else
            {
                listener.Next.Previous = listener.Previous;
            }

            listener.Previous = listener.Next = null;
        }

        private sealed class NormalDispatchNode : DispatchNode
        {
            private readonly Signal owner;

            internal NormalDispatchNode(Signal owner)
            {
                this.owner = owner;
            }

            internal override void Invoke()
            {
                this.owner.DispatchNormalListeners();
            }
        }

        private sealed class ActionListener : SignalListener
        {
            private Action callback;

            internal ActionListener(Action callback)
            {
                this.callback = callback;
            }

            internal override void OnSignal()
            {
                this.callback?.Invoke();
            }

            internal override void OnDetached()
            {
                this.callback = null;
            }
        }
    }
}
