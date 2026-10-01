// <copyright file="ReactiveProperty.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive
{
    using System.Collections.Generic;

    /// <summary>Represents a mutable reactive value with equality-filtered notifications.</summary>
    /// <typeparam name="T">The property value type.</typeparam>
    public sealed class ReactiveProperty<T> : IReactiveProperty<T>
    {
        private readonly IEqualityComparer<T> comparer;
        private T value;

        /// <summary>Initializes a new instance of the <see cref="ReactiveProperty{T}"/> class.</summary>
        /// <param name="value">The initial value.</param>
        /// <param name="comparer">The comparer used to suppress equal-value notifications.</param>
        public ReactiveProperty(T value, IEqualityComparer<T> comparer = null)
        {
            this.value = value;
            this.comparer = comparer ?? EqualityComparer<T>.Default;
        }

        /// <inheritdoc/>
        public T Value
        {
            get
            {
                this.OnChanged.Track();
                return this.value;
            }

            set
            {
                if (this.comparer.Equals(this.value, value))
                {
                    return;
                }

                this.value = value;
                this.OnChanged.Notify();
            }
        }

        /// <inheritdoc/>
        Signal IReactiveSource.OnChanged => this.OnChanged;

        /// <summary>Gets the signal raised when the value changes.</summary>
        internal Signal OnChanged { get; } = new Signal();

        /// <summary>Gets the current value without recording a reactive dependency.</summary>
        /// <returns>The current value.</returns>
        public T Peek()
        {
            return this.value;
        }

        /// <summary>Notifies observers even when the stored value has not changed.</summary>
        public void ForceNotify()
        {
            this.OnChanged.Notify();
        }
    }
}
