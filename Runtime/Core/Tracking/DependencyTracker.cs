// <copyright file="DependencyTracker.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive
{
    using System;
    using System.Collections.Generic;

    /// <summary>Retains unchanged subscriptions and avoids hash work while dependency order is stable.</summary>
    internal sealed class DependencyTracker : IDisposable
    {
        internal readonly DependencyCollector Reads = new DependencyCollector();
        private readonly Dictionary<Signal, IDisposable> subscriptions = new Dictionary<Signal, IDisposable>();
        private readonly List<Signal> signals = new List<Signal>(4);
        private readonly Action trigger;
        private bool requiresCurrentCheck;

        internal DependencyTracker(Action trigger)
        {
            this.trigger = trigger;
        }

        internal List<Signal> Signals => this.signals;

        internal bool RequiresCurrentCheck => this.requiresCurrentCheck;

        public void Dispose()
        {
            foreach (var handle in this.subscriptions.Values)
            {
                handle.Dispose();
            }

            this.subscriptions.Clear();
            this.signals.Clear();
            this.Reads.Clear();
            this.requiresCurrentCheck = false;
        }

        internal void Add(Signal signal)
        {
            if (this.subscriptions.ContainsKey(signal))
            {
                return;
            }

            this.subscriptions.Add(signal, signal.Subscribe(this.trigger, true));
            this.signals.Add(signal);
            this.requiresCurrentCheck |= signal.EnsureCurrent != null;
        }

        internal void BeginCapture()
        {
            this.Reads.BeginCapture(this.signals);
        }

        internal bool EndCapture()
        {
            return this.Reads.EndCapture();
        }

        internal void EnsureCurrent()
        {
            for (int i = 0; i < this.signals.Count; ++i)
            {
                this.signals[i].EnsureCurrent?.Invoke();
            }
        }

        internal void Synchronize()
        {
#if UNITY_5_3_OR_NEWER
            bool profiling = ReactiveProfiler.Enabled;
            if (profiling)
            {
                ReactiveProfiler.DependencySynchronize.Begin();
            }

            try
            {
#endif
                if (this.Reads.Matches(this.signals))
                {
                    this.Reads.Clear();
                    return;
                }

                for (int i = this.signals.Count - 1; i >= 0; --i)
                {
                    var signal = this.signals[i];
                    if (this.Reads.Contains(signal))
                    {
                        continue;
                    }

                    this.subscriptions[signal].Dispose();
                    this.subscriptions.Remove(signal);
                    this.signals.RemoveAt(i);
                }

                for (int i = 0; i < this.Reads.Count; ++i)
                {
                    var signal = this.Reads[i];
                    if (!this.subscriptions.ContainsKey(signal))
                    {
                        this.subscriptions.Add(signal, signal.Subscribe(this.trigger, true));
                    }
                }

                this.signals.Clear();
                for (int i = 0; i < this.Reads.Count; ++i)
                {
                    this.signals.Add(this.Reads[i]);
                }

                this.Reads.Clear();
                this.requiresCurrentCheck = false;
                for (int i = 0; i < this.signals.Count; ++i)
                {
                    if (this.signals[i].EnsureCurrent != null)
                    {
                        this.requiresCurrentCheck = true;
                        break;
                    }
                }
#if UNITY_5_3_OR_NEWER
            }
            finally
            {
                if (profiling)
                {
                    ReactiveProfiler.DependencySynchronize.End();
                }
            }
#endif
        }
    }
}
