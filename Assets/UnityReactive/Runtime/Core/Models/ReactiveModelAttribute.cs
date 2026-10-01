// <copyright file="ReactiveModelAttribute.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive
{
    using System;

    /// <summary>
    /// Generates tracked properties for all eligible instance fields declared by a partial class.
    /// </summary>
    /// <remarks>
    /// Apply <see cref="NonReactiveAttribute"/> to fields that should be excluded. A field-level
    /// <see cref="ReactiveAttribute"/> can still provide an explicit generated property name.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ReactiveModelAttribute : Attribute
    {
    }
}
