// <copyright file="IReactiveSource.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive
{
    /// <summary>Represents a source that can be tracked by the reactive dependency graph.</summary>
    public interface IReactiveSource
    {
        /// <summary>Gets the signal raised when the source changes.</summary>
        internal Signal OnChanged { get; }
    }
}
