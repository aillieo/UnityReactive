// <copyright file="KeySignals.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive
{
    using System;
    using System.Collections.Generic;

    /// <summary>Controls how per-key signals are retained after they are no longer observed.</summary>
    public enum KeySignalCachePolicy
    {
        /// <summary>Allows unused signals to be reclaimed by the garbage collector.</summary>
        Weak,

        /// <summary>Retains every created signal for the lifetime of its reactive collection.</summary>
        Strong,
    }

    // Per-key signal storage is selected once so the hot path never changes retention semantics.
    internal sealed class KeySignals<TKey>
    {
        private static readonly bool KeyCanBeNull =
            !typeof(TKey).IsValueType || Nullable.GetUnderlyingType(typeof(TKey)) != null;
        private readonly Dictionary<TKey, WeakReference<Signal>> weakSignals;
        private readonly Dictionary<TKey, Signal> strongSignals;
        private WeakReference<Signal> weakNullSignal;
        private Signal strongNullSignal;
        private List<TKey> deadSignals;
        private int createdSinceCleanup;

        internal KeySignals(IEqualityComparer<TKey> comparer = null)
            : this(KeySignalCachePolicy.Weak, comparer)
        {
        }

        internal KeySignals(KeySignalCachePolicy cachePolicy, IEqualityComparer<TKey> comparer = null)
        {
            switch (cachePolicy)
            {
                case KeySignalCachePolicy.Weak:
                    this.weakSignals = new Dictionary<TKey, WeakReference<Signal>>(comparer);
                    break;
                case KeySignalCachePolicy.Strong:
                    this.strongSignals = new Dictionary<TKey, Signal>(comparer);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(cachePolicy), cachePolicy, null);
            }
        }

        internal void Track(TKey key)
        {
            if (Tracking.Current == null)
            {
                return;
            }

            bool isNull = KeyCanBeNull && ReferenceEquals(key, null);
            if (this.strongSignals != null)
            {
                Signal strongSignal;
                if (isNull)
                {
                    strongSignal = this.strongNullSignal;
                }
                else
                {
                    this.strongSignals.TryGetValue(key, out strongSignal);
                }

                if (strongSignal == null)
                {
                    strongSignal = new Signal();
                    if (isNull)
                    {
                        this.strongNullSignal = strongSignal;
                    }
                    else
                    {
                        this.strongSignals[key] = strongSignal;
                    }
                }

                strongSignal.Track();
                return;
            }

            WeakReference<Signal> reference;
            if (isNull)
            {
                reference = this.weakNullSignal;
            }
            else
            {
                this.weakSignals.TryGetValue(key, out reference);
            }

            if (reference == null || !reference.TryGetTarget(out var signal))
            {
                signal = new Signal();
                reference = new WeakReference<Signal>(signal);
                if (isNull)
                {
                    this.weakNullSignal = reference;
                }
                else
                {
                    this.weakSignals[key] = reference;
                    if (++this.createdSinceCleanup == 64)
                    {
                        this.createdSinceCleanup = 0;
                        this.RemoveDeadSignals();
                    }
                }
            }

            signal.Track();
        }

        internal void Notify(TKey key)
        {
            bool isNull = KeyCanBeNull && ReferenceEquals(key, null);
            if (this.strongSignals != null)
            {
                Signal strongSignal;
                if (isNull)
                {
                    strongSignal = this.strongNullSignal;
                }
                else
                {
                    this.strongSignals.TryGetValue(key, out strongSignal);
                }

                strongSignal?.Notify();
                return;
            }

            WeakReference<Signal> reference;
            if (isNull)
            {
                reference = this.weakNullSignal;
            }
            else
            {
                this.weakSignals.TryGetValue(key, out reference);
            }

            if (reference == null)
            {
                return;
            }

            if (reference.TryGetTarget(out var signal))
            {
                signal.Notify();
                return;
            }

            if (isNull)
            {
                this.weakNullSignal = null;
            }
            else
            {
                this.weakSignals.Remove(key);
            }
        }

        private void RemoveDeadSignals()
        {
            var dead = this.deadSignals ?? (this.deadSignals = new List<TKey>());
            foreach (var pair in this.weakSignals)
            {
                if (!pair.Value.TryGetTarget(out _))
                {
                    dead.Add(pair.Key);
                }
            }

            foreach (var item in dead)
            {
                this.weakSignals.Remove(item);
            }

            dead.Clear();
        }
    }
}
