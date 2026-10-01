// <copyright file="PropertyChange.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive
{
    /// <summary>Describes a value delivered by a reactive property subscription.</summary>
    /// <typeparam name="T">The property value type.</typeparam>
    public readonly struct PropertyChange<T>
    {
        internal PropertyChange(bool hasPreviousValue, T oldValue, T newValue)
        {
            this.HasPreviousValue = hasPreviousValue;
            this.OldValue = oldValue;
            this.NewValue = newValue;
        }

        /// <summary>Gets a value indicating whether <see cref="OldValue"/> contains a previously delivered value.</summary>
        public bool HasPreviousValue { get; }

        /// <summary>Gets the previously delivered value, or the default value on the initial notification.</summary>
        public T OldValue { get; }

        /// <summary>Gets the value delivered by the current notification.</summary>
        public T NewValue { get; }
    }
}
