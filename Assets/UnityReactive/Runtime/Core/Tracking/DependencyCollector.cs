// <copyright file="DependencyCollector.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>Reusable dependency collection optimized for the common one-to-eight input case.</summary>
    internal sealed class DependencyCollector
    {
        private const int HashThreshold = 8;
        private readonly List<Signal> signals = new List<Signal>(4);
        private HashSet<Signal> lookup;
        private List<Signal> expected;
        private int expectedIndex;

        internal int Count => this.signals.Count;

        internal Signal this[int index] => this.signals[index];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Add(Signal signal)
        {
            var currentExpected = this.expected;
            if (currentExpected != null)
            {
                int index = this.expectedIndex;
                if (index < currentExpected.Count && ReferenceEquals(currentExpected[index], signal))
                {
                    this.expectedIndex = index + 1;
                    return;
                }

                this.AddUnexpected(signal, currentExpected, index);
                return;
            }

            this.AddMaterialized(signal);
        }

        internal void BeginCapture(List<Signal> current)
        {
            this.signals.Clear();
            this.lookup?.Clear();
            this.expected = current;
            this.expectedIndex = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool EndCapture()
        {
            if (this.expected == null)
            {
                return false;
            }

            if (this.expectedIndex == this.expected.Count)
            {
                this.expected = null;
                this.expectedIndex = 0;
                return true;
            }

            this.MaterializeExpectedPrefix();
            return false;
        }

        internal bool Contains(Signal signal)
        {
            if (this.lookup != null)
            {
                return this.lookup.Contains(signal);
            }

            for (int i = 0; i < this.signals.Count; ++i)
            {
                if (ReferenceEquals(this.signals[i], signal))
                {
                    return true;
                }
            }

            return false;
        }

        internal bool Matches(List<Signal> other)
        {
            if (this.signals.Count != other.Count)
            {
                return false;
            }

            for (int i = 0; i < this.signals.Count; ++i)
            {
                if (!ReferenceEquals(this.signals[i], other[i]))
                {
                    return false;
                }
            }

            return true;
        }

        internal void Remove(Signal signal)
        {
            for (int i = 0; i < this.signals.Count; ++i)
            {
                if (!ReferenceEquals(this.signals[i], signal))
                {
                    continue;
                }

                this.signals.RemoveAt(i);
                this.lookup?.Remove(signal);
                return;
            }
        }

        internal void Clear()
        {
            this.expected = null;
            this.expectedIndex = 0;
            this.signals.Clear();
            this.lookup?.Clear();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AddUnexpected(Signal signal, List<Signal> currentExpected, int index)
        {
            // Repeated reads do not change the dependency order.
            for (int i = 0; i < index; ++i)
            {
                if (ReferenceEquals(currentExpected[i], signal))
                {
                    return;
                }
            }

            this.MaterializeExpectedPrefix();
            this.AddMaterialized(signal);
        }

        private void AddMaterialized(Signal signal)
        {
            if (this.lookup != null)
            {
                if (this.lookup.Add(signal))
                {
                    this.signals.Add(signal);
                }

                return;
            }

            for (int i = 0; i < this.signals.Count; ++i)
            {
                if (ReferenceEquals(this.signals[i], signal))
                {
                    return;
                }
            }

            this.signals.Add(signal);
            if (this.signals.Count == HashThreshold)
            {
                this.lookup = new HashSet<Signal>(this.signals);
            }
        }

        private void MaterializeExpectedPrefix()
        {
            var source = this.expected;
            this.expected = null;
            for (int i = 0; i < this.expectedIndex; ++i)
            {
                this.signals.Add(source[i]);
            }

            if (this.lookup != null)
            {
                foreach (var signal in this.signals)
                {
                    this.lookup.Add(signal);
                }
            }
            else if (this.signals.Count >= HashThreshold)
            {
                this.lookup = new HashSet<Signal>(this.signals);
            }

            this.expectedIndex = 0;
        }
    }
}
