// <copyright file="EventSignal.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive
{
    using System;
    using System.Collections.Generic;

    /// <summary>Publishes typed events synchronously to disposable subscribers.</summary>
    /// <typeparam name="T">The event payload type.</typeparam>
    /// <remarks>
    /// Unlike <see cref="Signal"/>, events are not dependency-tracked or coalesced by <see cref="Rx.Batch()"/>.
    /// </remarks>
    public sealed class EventSignal<T> : IEventStream<T>
    {
        private Subscription first;
        private Subscription last;
        private int listenerCount;
        private int traversalDepth;
        private bool needsCleanup;

        /// <summary>Gets the number of active subscribers.</summary>
        public int ListenerCount => this.listenerCount;

        /// <inheritdoc/>
        public IDisposable Subscribe(Action<T> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var subscription = new Subscription(this, handler)
            {
                Previous = this.last,
            };

            if (this.last == null)
            {
                this.first = subscription;
            }
            else
            {
                this.last.Next = subscription;
            }

            this.last = subscription;
            ++this.listenerCount;
            return subscription;
        }

        /// <summary>Synchronously delivers one event to every current subscriber.</summary>
        /// <param name="value">The event payload.</param>
        public void Emit(T value)
        {
            if (this.listenerCount == 0)
            {
                return;
            }

            List<Exception> errors = null;
            ++this.traversalDepth;
            var boundary = this.last;
            try
            {
                for (var subscription = this.first; subscription != null; subscription = subscription.Next)
                {
                    if (subscription.Owner != null)
                    {
                        try
                        {
                            subscription.Handler(value);
                        }
                        catch (Exception error)
                        {
                            AddError(ref errors, error);
                        }
                    }

                    if (subscription == boundary)
                    {
                        break;
                    }
                }
            }
            finally
            {
                this.EndTraversal();
            }

            if (errors != null)
            {
                throw new AggregateException("Event handlers failed.", errors);
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

        private void Detach(Subscription subscription)
        {
            if (!ReferenceEquals(subscription.Owner, this))
            {
                return;
            }

            subscription.Owner = null;
            subscription.Handler = null;
            --this.listenerCount;
            if (this.traversalDepth == 0)
            {
                this.Unlink(subscription);
            }
            else
            {
                this.needsCleanup = true;
            }
        }

        private void EndTraversal()
        {
            if (--this.traversalDepth != 0 || !this.needsCleanup)
            {
                return;
            }

            this.needsCleanup = false;
            var subscription = this.first;
            while (subscription != null)
            {
                var next = subscription.Next;
                if (subscription.Owner == null)
                {
                    this.Unlink(subscription);
                }

                subscription = next;
            }
        }

        private void Unlink(Subscription subscription)
        {
            if (subscription.Previous == null)
            {
                this.first = subscription.Next;
            }
            else
            {
                subscription.Previous.Next = subscription.Next;
            }

            if (subscription.Next == null)
            {
                this.last = subscription.Previous;
            }
            else
            {
                subscription.Next.Previous = subscription.Previous;
            }

            subscription.Previous = subscription.Next = null;
        }

        private sealed class Subscription : IDisposable
        {
            internal EventSignal<T> Owner;
            internal Subscription Previous;
            internal Subscription Next;
            internal Action<T> Handler;

            internal Subscription(EventSignal<T> owner, Action<T> handler)
            {
                this.Owner = owner;
                this.Handler = handler;
            }

            public void Dispose()
            {
                this.Owner?.Detach(this);
            }
        }
    }
}
