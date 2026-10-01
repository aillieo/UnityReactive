// <copyright file="ReactiveSet.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>Provides an observable set whose structural changes participate in the reactive graph.</summary>
    /// <typeparam name="T">The item type.</typeparam>
    public sealed class ReactiveSet<T> : ISet<T>, IReadOnlyCollection<T>, IReactiveSource
    {
        private readonly HashSet<T> items;
        private readonly KeySignals<T> elements;

        /// <summary>Initializes a new instance of the <see cref="ReactiveSet{T}"/> class.</summary>
        /// <param name="comparer">The item comparer, or <see langword="null"/> for the default comparer.</param>
        public ReactiveSet(IEqualityComparer<T> comparer = null)
            : this(KeySignalCachePolicy.Weak, comparer)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="ReactiveSet{T}"/> class.</summary>
        /// <param name="keySignalCachePolicy">The policy used to retain per-item signals.</param>
        /// <param name="comparer">The item comparer, or <see langword="null"/> for the default comparer.</param>
        public ReactiveSet(KeySignalCachePolicy keySignalCachePolicy, IEqualityComparer<T> comparer = null)
        {
            this.items = new HashSet<T>(comparer);
            this.elements = new KeySignals<T>(keySignalCachePolicy, comparer);
        }

        /// <inheritdoc/>
        public int Count
        {
            get
            {
                this.OnChanged.Track();
                return this.items.Count;
            }
        }

        /// <inheritdoc/>
        public bool IsReadOnly => false;

        /// <inheritdoc/>
        Signal IReactiveSource.OnChanged => this.OnChanged;

        /// <summary>Gets the signal raised when the set changes.</summary>
        internal Signal OnCollectionChanged { get; } = new Signal();

        /// <summary>Gets the signal raised when the set changes.</summary>
        internal Signal OnChanged => this.OnCollectionChanged;

        /// <inheritdoc/>
        public bool Contains(T item)
        {
            this.elements.Track(item);
            return this.items.Contains(item);
        }

        /// <inheritdoc/>
        public bool Add(T item)
        {
            if (!this.items.Add(item))
            {
                return false;
            }

            this.Changed(item);
            return true;
        }

        /// <inheritdoc/>
        void ICollection<T>.Add(T item)
        {
            this.Add(item);
        }

        /// <inheritdoc/>
        public bool Remove(T item)
        {
            if (!this.items.Remove(item))
            {
                return false;
            }

            this.Changed(item);
            return true;
        }

        /// <inheritdoc/>
        public void Clear()
        {
            if (this.items.Count == 0)
            {
                return;
            }

            var old = new List<T>(this.items);
            this.items.Clear();
            using (Rx.Batch())
            {
                foreach (var item in old)
                {
                    this.elements.Notify(item);
                }

                this.OnChanged.Notify();
            }
        }

        /// <inheritdoc/>
        public void UnionWith(IEnumerable<T> other)
        {
            this.Mutate(other, (set, values) => set.UnionWith(values));
        }

        /// <inheritdoc/>
        public void IntersectWith(IEnumerable<T> other)
        {
            this.Mutate(other, (set, values) => set.IntersectWith(values));
        }

        /// <inheritdoc/>
        public void ExceptWith(IEnumerable<T> other)
        {
            this.Mutate(other, (set, values) => set.ExceptWith(values));
        }

        /// <inheritdoc/>
        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            this.Mutate(other, (set, values) => set.SymmetricExceptWith(values));
        }

        /// <inheritdoc/>
        public bool IsSubsetOf(IEnumerable<T> other)
        {
            this.OnChanged.Track();
            return this.items.IsSubsetOf(other);
        }

        /// <inheritdoc/>
        public bool IsSupersetOf(IEnumerable<T> other)
        {
            this.OnChanged.Track();
            return this.items.IsSupersetOf(other);
        }

        /// <inheritdoc/>
        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            this.OnChanged.Track();
            return this.items.IsProperSubsetOf(other);
        }

        /// <inheritdoc/>
        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            this.OnChanged.Track();
            return this.items.IsProperSupersetOf(other);
        }

        /// <inheritdoc/>
        public bool Overlaps(IEnumerable<T> other)
        {
            this.OnChanged.Track();
            return this.items.Overlaps(other);
        }

        /// <inheritdoc/>
        public bool SetEquals(IEnumerable<T> other)
        {
            this.OnChanged.Track();
            return this.items.SetEquals(other);
        }

        /// <inheritdoc/>
        public void CopyTo(T[] array, int arrayIndex)
        {
            this.OnChanged.Track();
            this.items.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator()
        {
            this.OnChanged.Track();
            return this.items.GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private void Changed(T item)
        {
            using (Rx.Batch())
            {
                this.elements.Notify(item);
                this.OnChanged.Notify();
            }
        }

        private void Mutate(IEnumerable<T> other, Action<HashSet<T>, IEnumerable<T>> mutation)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            var next = new HashSet<T>(this.items, this.items.Comparer);
            mutation(next, other); // Enumerate and validate before changing observable state.
            var changes = new HashSet<T>(this.items, this.items.Comparer);
            changes.SymmetricExceptWith(next);
            if (changes.Count == 0)
            {
                return;
            }

            this.items.Clear();
            this.items.UnionWith(next);
            using (Rx.Batch())
            {
                foreach (var item in changes)
                {
                    this.elements.Notify(item);
                }

                this.OnChanged.Notify();
            }
        }
    }
}
