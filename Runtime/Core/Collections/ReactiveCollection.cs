// <copyright file="ReactiveCollection.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>Provides an observable list whose structural changes participate in the reactive graph.</summary>
    /// <typeparam name="T">The item type.</typeparam>
    public sealed class ReactiveCollection<T> : IList<T>, IReadOnlyList<T>, IReactiveSource
    {
        private readonly List<T> items;
        private readonly KeySignals<int> elements;
        private readonly IEqualityComparer<T> comparer;

        /// <summary>Initializes a new instance of the <see cref="ReactiveCollection{T}"/> class.</summary>
        /// <param name="values">The initial values, or <see langword="null"/> for an empty collection.</param>
        /// <param name="comparer">The value comparer, or <see langword="null"/> for the default comparer.</param>
        public ReactiveCollection(IEnumerable<T> values = null, IEqualityComparer<T> comparer = null)
            : this(KeySignalCachePolicy.Weak, values, comparer)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="ReactiveCollection{T}"/> class.</summary>
        /// <param name="keySignalCachePolicy">The policy used to retain per-index signals.</param>
        /// <param name="values">The initial values, or <see langword="null"/> for an empty collection.</param>
        /// <param name="comparer">The value comparer, or <see langword="null"/> for the default comparer.</param>
        public ReactiveCollection(
            KeySignalCachePolicy keySignalCachePolicy,
            IEnumerable<T> values = null,
            IEqualityComparer<T> comparer = null)
        {
            this.items = values == null ? new List<T>() : new List<T>(values);
            this.elements = new KeySignals<int>(keySignalCachePolicy);
            this.comparer = comparer ?? EqualityComparer<T>.Default;
        }

        /// <inheritdoc/>
        public int Count
        {
            get
            {
                this.OnCollectionChanged.Track();
                return this.items.Count;
            }
        }

        /// <inheritdoc/>
        public bool IsReadOnly => false;

        /// <inheritdoc/>
        Signal IReactiveSource.OnChanged => this.OnChanged;

        /// <summary>Gets the signal raised when the collection structure changes.</summary>
        internal Signal OnCollectionChanged { get; } = new Signal();

        /// <summary>Gets the signal raised when the collection structure or an item changes.</summary>
        internal Signal OnChanged { get; } = new Signal();

        /// <inheritdoc/>
        public T this[int index]
        {
            get
            {
                this.elements.Track(index);
                return this.items[index];
            }

            set
            {
                if (this.comparer.Equals(this.items[index], value))
                {
                    return;
                }

                this.items[index] = value;
                using (Rx.Batch())
                {
                    this.elements.Notify(index);
                    this.OnChanged.Notify();
                }
            }
        }

        /// <summary>Attempts to read an item without throwing when the index is outside the collection.</summary>
        /// <param name="index">The zero-based index to read.</param>
        /// <param name="value">Receives the item when the index exists; otherwise, the default value.</param>
        /// <returns><see langword="true"/> when the index exists; otherwise, <see langword="false"/>.</returns>
        public bool TryGetValue(int index, out T value)
        {
            this.elements.Track(index);
            if (index >= 0 && index < this.items.Count)
            {
                value = this.items[index];
                return true;
            }

            value = default;
            return false;
        }

        /// <inheritdoc/>
        public void Add(T item)
        {
            this.Insert(this.items.Count, item);
        }

        /// <inheritdoc/>
        public void Insert(int index, T item)
        {
            this.items.Insert(index, item);
            this.ChangedRange(index, this.items.Count, true);
        }

        /// <inheritdoc/>
        public void RemoveAt(int index)
        {
            int oldCount = this.items.Count;
            this.items.RemoveAt(index);
            this.ChangedRange(index, oldCount, true);
        }

        /// <inheritdoc/>
        public bool Remove(T item)
        {
            int index = this.FindIndex(item);
            if (index < 0)
            {
                return false;
            }

            this.RemoveAt(index);
            return true;
        }

        /// <inheritdoc/>
        public void Clear()
        {
            int oldCount = this.items.Count;
            if (oldCount == 0)
            {
                return;
            }

            this.items.Clear();
            this.ChangedRange(0, oldCount, true);
        }

        /// <summary>Appends a sequence of items and emits one batched change.</summary>
        /// <param name="values">The items to append.</param>
        public void AddRange(IEnumerable<T> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            var copy = new List<T>(values);
            if (copy.Count == 0)
            {
                return;
            }

            int start = this.items.Count;
            this.items.AddRange(copy);
            this.ChangedRange(start, this.items.Count, true);
        }

        /// <summary>Removes a range and inserts replacement values at the same position.</summary>
        /// <param name="start">The start index, or a negative offset from the end.</param>
        /// <param name="deleteCount">The maximum number of items to remove.</param>
        /// <param name="values">The replacement values.</param>
        /// <returns>The removed items.</returns>
        public T[] Splice(int start, int deleteCount, params T[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            int oldCount = this.items.Count;
            start = start < 0 ? Math.Max(oldCount + start, 0) : Math.Min(start, oldCount);
            deleteCount = Math.Max(0, Math.Min(deleteCount, oldCount - start));
            var removed = this.items.GetRange(start, deleteCount).ToArray();
            if (deleteCount == 0 && values.Length == 0)
            {
                return removed;
            }

            this.items.RemoveRange(start, deleteCount);
            this.items.InsertRange(start, values);
            this.ChangedRange(start, Math.Max(oldCount, this.items.Count), oldCount != this.items.Count);
            return removed;
        }

        /// <summary>Sorts the collection and notifies only indices whose values changed.</summary>
        /// <param name="sortComparer">The comparer to use, or <see langword="null"/> for the default comparer.</param>
        public void Sort(IComparer<T> sortComparer = null)
        {
            var sorted = new List<T>(this.items);
            sorted.Sort(sortComparer);
            this.ReplaceOrder(sorted);
        }

        /// <summary>Reverses the item order and notifies only indices whose values changed.</summary>
        public void Reverse()
        {
            var reversed = new List<T>(this.items);
            reversed.Reverse();
            this.ReplaceOrder(reversed);
        }

        /// <inheritdoc/>
        public bool Contains(T item)
        {
            return this.IndexOf(item) >= 0;
        }

        /// <inheritdoc/>
        public int IndexOf(T item)
        {
            this.OnChanged.Track();
            return this.FindIndex(item);
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
            this.OnCollectionChanged.Track();
            int index = 0;
            foreach (var item in this.items)
            {
                this.elements.Track(index++);
                yield return item;
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private void ReplaceOrder(List<T> next)
        {
            var changed = new List<int>();
            for (int i = 0; i < this.items.Count; ++i)
            {
                if (!this.comparer.Equals(this.items[i], next[i]))
                {
                    changed.Add(i);
                }
            }

            this.items.Clear();
            this.items.AddRange(next);
            if (changed.Count == 0)
            {
                return;
            }

            using (Rx.Batch())
            {
                foreach (int i in changed)
                {
                    this.elements.Notify(i);
                }

                this.OnChanged.Notify();
            }
        }

        private void ChangedRange(int start, int end, bool structure)
        {
            using (Rx.Batch())
            {
                for (int i = start; i < end; ++i)
                {
                    this.elements.Notify(i);
                }

                if (structure)
                {
                    this.OnCollectionChanged.Notify();
                }

                this.OnChanged.Notify();
            }
        }

        private int FindIndex(T item)
        {
            var values = this.items;
            var equalityComparer = this.comparer;
            for (int i = 0; i < values.Count; ++i)
            {
                if (equalityComparer.Equals(values[i], item))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
