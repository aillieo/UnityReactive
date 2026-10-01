// <copyright file="ReactiveAttribute.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive
{
    using System;

    /// <summary>Generates a tracked property for an instance field in a partial class.</summary>
    /// <example>
    /// Declare the containing type as <see langword="partial"/> and annotate its backing fields:
    /// <code>
    /// public sealed partial class PlayerModel
    /// {
    ///     [Reactive]
    ///     private int health = 100;
    ///
    ///     partial void OnHealthChanged(int oldValue, int newValue)
    ///     {
    ///         // React to a generated Health property change.
    ///     }
    /// }
    /// </code>
    /// The source generator creates the tracked <c>Health</c> property and its observation helpers.
    /// </example>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class ReactiveAttribute : Attribute
    {
        /// <summary>Initializes a new instance of the <see cref="ReactiveAttribute"/> class.</summary>
        public ReactiveAttribute()
        {
        }

        /// <summary>Initializes a new instance of the <see cref="ReactiveAttribute"/> class.</summary>
        /// <param name="propertyName">The generated property name.</param>
        public ReactiveAttribute(string propertyName)
        {
            this.PropertyName = propertyName;
        }

        /// <summary>Gets the explicit generated property name, or <see langword="null"/> to infer it.</summary>
        public string PropertyName { get; }
    }
}
