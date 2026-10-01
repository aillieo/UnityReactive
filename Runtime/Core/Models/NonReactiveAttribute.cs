// <copyright file="NonReactiveAttribute.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive
{
    using System;

    /// <summary>
    /// Excludes a field from automatic property generation in a <see cref="ReactiveModelAttribute"/> class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class NonReactiveAttribute : Attribute
    {
    }
}
