// <copyright file="Unit.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive
{
    /// <summary>Represents an event that carries no data.</summary>
    public readonly struct Unit
    {
        /// <summary>Gets the single meaningful value of <see cref="Unit"/>.</summary>
        public static Unit Default => default;
    }
}
