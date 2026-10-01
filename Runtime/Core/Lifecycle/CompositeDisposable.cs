// <copyright file="CompositeDisposable.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive
{
    using System;
    using System.Collections.Generic;

    /// <summary>Owns a group of disposable resources and releases them together.</summary>
    public sealed class CompositeDisposable : IDisposable
    {
        private static List<IDisposable> pool;

        private readonly List<IDisposable> items;
        private bool disposed;

        /// <summary>Initializes a new instance of the <see cref="CompositeDisposable"/> class.</summary>
        /// <param name="capacity">The initial resource capacity.</param>
        public CompositeDisposable(int capacity = 0)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            this.items = new List<IDisposable>(capacity);
        }

        /// <summary>Gets the number of resources currently owned.</summary>
        public int Count => this.items.Count;

        /// <summary>Gets a value indicating whether this owner has been permanently disposed.</summary>
        public bool IsDisposed => this.disposed;

        /// <summary>Adds a resource, or disposes it immediately if this owner is already disposed.</summary>
        /// <typeparam name="T">The disposable resource type.</typeparam>
        /// <param name="item">The resource to own.</param>
        /// <returns>The supplied resource.</returns>
        public T Add<T>(T item)
            where T : IDisposable
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (this.disposed)
            {
                item.Dispose();
            }
            else
            {
                this.items.Add(item);
            }

            return item;
        }

        /// <summary>Removes and disposes the first matching resource.</summary>
        /// <param name="item">The resource to remove and dispose.</param>
        /// <returns><see langword="true"/> when the resource was owned and removed; otherwise, <see langword="false"/>.</returns>
        public bool Remove(IDisposable item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (this.disposed)
            {
                return false;
            }

            int index = this.items.IndexOf(item);
            if (index < 0)
            {
                return false;
            }

            this.items.RemoveAt(index);
            item.Dispose();
            return true;
        }

        /// <summary>Releases the current resources while keeping this owner reusable.</summary>
        public void Clear()
        {
            if (this.disposed || this.items.Count == 0)
            {
                return;
            }

            DisposeAll(this.items);
        }

        /// <summary>Permanently closes this owner and releases all current resources.</summary>
        /// <remarks>Resources are disposed in reverse insertion order.</remarks>
        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            if (this.items.Count == 0)
            {
                return;
            }

            DisposeAll(this.items);
        }

        private static void DisposeAll(List<IDisposable> items)
        {
            List<IDisposable> snapshot;
            if (pool != null)
            {
                snapshot = pool;
                pool = null;
            }
            else
            {
                snapshot = new List<IDisposable>(items.Count);
            }

            snapshot.AddRange(items);
            items.Clear();

            List<Exception> errors = null;
            for (int i = snapshot.Count - 1; i >= 0; --i)
            {
                try
                {
                    snapshot[i].Dispose();
                }
                catch (Exception error)
                {
                    (errors ?? (errors = new List<Exception>())).Add(error);
                }
            }

            if (pool == null)
            {
                snapshot.Clear();
                pool = snapshot;
            }

            if (errors != null)
            {
                throw new AggregateException("One or more resources failed to dispose.", errors);
            }
        }
    }
}
