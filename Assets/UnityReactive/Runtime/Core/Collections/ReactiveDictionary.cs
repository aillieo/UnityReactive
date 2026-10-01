// <copyright file="ReactiveDictionary.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive
{
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>Provides an observable dictionary whose structural and value changes participate in the reactive graph.</summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    public sealed class ReactiveDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IReactiveSource
    {
        private readonly Dictionary<TKey, TValue> items;
        private readonly KeySignals<TKey> elements;
        private readonly IEqualityComparer<TValue> valueComparer;

        /// <summary>Initializes a new instance of the <see cref="ReactiveDictionary{TKey, TValue}"/> class.</summary>
        /// <param name="keyComparer">The key comparer, or <see langword="null"/> for the default comparer.</param>
        /// <param name="valueComparer">The value comparer, or <see langword="null"/> for the default comparer.</param>
        public ReactiveDictionary(IEqualityComparer<TKey> keyComparer = null, IEqualityComparer<TValue> valueComparer = null)
            : this(KeySignalCachePolicy.Weak, keyComparer, valueComparer)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="ReactiveDictionary{TKey, TValue}"/> class.</summary>
        /// <param name="keySignalCachePolicy">The policy used to retain per-key signals.</param>
        /// <param name="keyComparer">The key comparer, or <see langword="null"/> for the default comparer.</param>
        /// <param name="valueComparer">The value comparer, or <see langword="null"/> for the default comparer.</param>
        public ReactiveDictionary(
            KeySignalCachePolicy keySignalCachePolicy,
            IEqualityComparer<TKey> keyComparer = null,
            IEqualityComparer<TValue> valueComparer = null)
        {
            this.items = new Dictionary<TKey, TValue>(keyComparer);
            this.elements = new KeySignals<TKey>(keySignalCachePolicy, keyComparer);
            this.valueComparer = valueComparer ?? EqualityComparer<TValue>.Default;
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

        /// <summary>Gets a tracked snapshot of the dictionary keys.</summary>
        /// <remarks>Returning a snapshot prevents an untracked live view from escaping.</remarks>
        public ICollection<TKey> Keys
        {
            get
            {
                this.OnCollectionChanged.Track();
                return new List<TKey>(this.items.Keys).AsReadOnly();
            }
        }

        /// <summary>Gets a tracked snapshot of the dictionary values.</summary>
        /// <remarks>Returning a snapshot prevents an untracked live view from escaping.</remarks>
        public ICollection<TValue> Values
        {
            get
            {
                this.OnChanged.Track();
                return new List<TValue>(this.items.Values).AsReadOnly();
            }
        }

        /// <inheritdoc/>
        Signal IReactiveSource.OnChanged => this.OnChanged;

        /// <inheritdoc/>
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => this.Keys;

        /// <inheritdoc/>
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => this.Values;

        /// <summary>Gets the signal raised when the dictionary structure changes.</summary>
        internal Signal OnCollectionChanged { get; } = new Signal();

        /// <summary>Gets the signal raised when the dictionary structure or a value changes.</summary>
        internal Signal OnChanged { get; } = new Signal();

        /// <inheritdoc/>
        public TValue this[TKey key]
        {
            get
            {
                this.elements.Track(key);
                return this.items[key];
            }

            set
            {
                bool existed = this.items.TryGetValue(key, out var old);
                if (existed && this.valueComparer.Equals(old, value))
                {
                    return;
                }

                this.items[key] = value;
                this.Changed(key, !existed);
            }
        }

        /// <inheritdoc/>
        public bool ContainsKey(TKey key)
        {
            this.elements.Track(key);
            return this.items.ContainsKey(key);
        }

        /// <inheritdoc/>
        public bool TryGetValue(TKey key, out TValue value)
        {
            this.elements.Track(key);
            return this.items.TryGetValue(key, out value);
        }

        /// <inheritdoc/>
        public void Add(TKey key, TValue value)
        {
            this.items.Add(key, value);
            this.Changed(key, true);
        }

        /// <inheritdoc/>
        public bool Remove(TKey key)
        {
            if (!this.items.Remove(key))
            {
                return false;
            }

            this.Changed(key, true);
            return true;
        }

        /// <inheritdoc/>
        public void Clear()
        {
            if (this.items.Count == 0)
            {
                return;
            }

            var keys = new List<TKey>(this.items.Keys);
            this.items.Clear();
            using (Rx.Batch())
            {
                foreach (var key in keys)
                {
                    this.elements.Notify(key);
                }

                this.OnCollectionChanged.Notify();
                this.OnChanged.Notify();
            }
        }

        /// <inheritdoc/>
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            this.Add(item.Key, item.Value);
        }

        /// <inheritdoc/>
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return this.TryGetValue(item.Key, out var value) && this.valueComparer.Equals(value, item.Value);
        }

        /// <inheritdoc/>
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (!this.items.TryGetValue(item.Key, out var value) || !this.valueComparer.Equals(value, item.Value))
            {
                return false;
            }

            return this.Remove(item.Key);
        }

        /// <inheritdoc/>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            this.OnChanged.Track();
            ((ICollection<KeyValuePair<TKey, TValue>>)this.items).CopyTo(array, arrayIndex);
        }

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            this.OnCollectionChanged.Track();
            foreach (var pair in this.items)
            {
                this.elements.Track(pair.Key);
                yield return pair;
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private void Changed(TKey key, bool structure)
        {
            using (Rx.Batch())
            {
                this.elements.Notify(key);
                if (structure)
                {
                    this.OnCollectionChanged.Notify();
                }

                this.OnChanged.Notify();
            }
        }
    }
}
