// <copyright file="IReadOnlyReactiveProperty.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive
{
    /// <summary>Exposes a readable reactive value and its change notifications.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    public interface IReadOnlyReactiveProperty<out T> : IReactiveSource
    {
        /// <summary>Gets the current value and records a dependency when tracking is active.</summary>
        T Value { get; }
    }
}
