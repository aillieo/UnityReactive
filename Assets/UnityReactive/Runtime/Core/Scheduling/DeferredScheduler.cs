// <copyright file="DeferredScheduler.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive
{
    using System;
    using System.Collections.Generic;

    /// <summary>Flush executes one snapshot. Newly enqueued work waits for the next flush.</summary>
    public sealed class DeferredScheduler : IReactiveScheduler
    {
        private static readonly Comparison<KeyValuePair<Action, long>> CompareGeneration = (a, b) => a.Value.CompareTo(b.Value);

        private readonly Dictionary<Action, long> pending = new Dictionary<Action, long>();
        private readonly List<KeyValuePair<Action, long>> snapshot = new List<KeyValuePair<Action, long>>();
        private long generation;
        private bool flushing;

        /// <summary>Gets the number of distinct effects waiting for the next flush.</summary>
        public int PendingCount => this.pending.Count;

        /// <summary>Gets or sets the callback that receives exceptions thrown by scheduled effects.</summary>
        /// <remarks>If no handler is assigned, <see cref="Flush"/> throws an <see cref="AggregateException"/>.</remarks>
        public Action<Exception> ExceptionHandler { get; set; }

        /// <inheritdoc/>
        public void Schedule(Action effect)
        {
            if (effect == null)
            {
                throw new ArgumentNullException(nameof(effect));
            }

            if (!this.pending.ContainsKey(effect))
            {
                this.pending.Add(effect, ++this.generation);
            }
        }

        /// <inheritdoc/>
        public void Cancel(Action effect)
        {
            if (effect != null)
            {
                this.pending.Remove(effect);
            }
        }

        /// <summary>Removes all pending effects without executing them.</summary>
        public void Clear()
        {
            this.pending.Clear();
        }

        /// <summary>Executes a snapshot of the currently pending effects in scheduling order.</summary>
        /// <remarks>Effects scheduled during a flush remain pending until the next flush.</remarks>
        public void Flush()
        {
            if (this.flushing || this.pending.Count == 0)
            {
                return;
            }

            this.flushing = true;
            foreach (var item in this.pending)
            {
                this.snapshot.Add(item);
            }

            this.snapshot.Sort(CompareGeneration);
            List<Exception> errors = null;
            try
            {
                foreach (var item in this.snapshot)
                {
                    if (!this.pending.TryGetValue(item.Key, out var token) || token != item.Value)
                    {
                        continue;
                    }

                    this.pending.Remove(item.Key);
                    try
                    {
                        Rx.Untracked(item.Key);
                    }
                    catch (Exception e)
                    {
                        (errors ?? (errors = new List<Exception>())).Add(e);
                    }
                }
            }
            finally
            {
                this.flushing = false;
                this.snapshot.Clear();
            }

            if (errors == null)
            {
                return;
            }

            if (this.ExceptionHandler == null)
            {
                throw new AggregateException(errors);
            }

            foreach (var error in errors)
            {
                this.ExceptionHandler(error);
            }
        }
    }
}
