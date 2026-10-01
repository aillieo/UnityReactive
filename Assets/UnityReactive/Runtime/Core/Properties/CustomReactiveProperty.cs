// <copyright file="CustomReactiveProperty.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive
{
    using System;

    /// <summary>Adapts custom getter and setter delegates to the reactive property contract.</summary>
    /// <typeparam name="T">The property value type.</typeparam>
    public sealed class CustomReactiveProperty<T> : IReactiveProperty<T>
    {
        private readonly Func<T> getter;
        private readonly Func<T, bool> setter;

        /// <summary>Initializes a new instance of the <see cref="CustomReactiveProperty{T}"/> class.</summary>
        /// <param name="getter">The delegate that reads the current value.</param>
        /// <param name="setter">The delegate that writes a value and returns whether observers should be notified.</param>
        public CustomReactiveProperty(Func<T> getter, Func<T, bool> setter)
        {
            this.getter = getter ?? throw new ArgumentNullException(nameof(getter));
            this.setter = setter ?? throw new ArgumentNullException(nameof(setter));
        }

        /// <inheritdoc/>
        public T Value
        {
            get
            {
                this.OnChanged.Track();
                return Rx.Untracked(this.getter);
            }

            set
            {
                if (Rx.Untracked(() => this.setter(value)))
                {
                    this.OnChanged.Notify();
                }
            }
        }

        /// <inheritdoc/>
        Signal IReactiveSource.OnChanged => this.OnChanged;

        /// <summary>Gets the signal raised when the custom value changes.</summary>
        internal Signal OnChanged { get; } = new Signal();

        /// <summary>Notifies observers without invoking the setter.</summary>
        public void ForceNotify()
        {
            this.OnChanged.Notify();
        }
    }
}
