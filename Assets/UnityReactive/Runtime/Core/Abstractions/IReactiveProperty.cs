// <copyright file="IReactiveProperty.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive
{
    /// <summary>Exposes a writable reactive value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    public interface IReactiveProperty<T> : IReadOnlyReactiveProperty<T>
    {
        /// <summary>Gets or sets the current value.</summary>
        new T Value { get; set; }
    }
}
