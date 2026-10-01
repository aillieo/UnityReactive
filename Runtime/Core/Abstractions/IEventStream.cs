// <copyright file="IEventStream.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive
{
    using System;

    /// <summary>Exposes a typed event stream that can be subscribed to.</summary>
    /// <typeparam name="T">The event payload type.</typeparam>
    public interface IEventStream<out T>
    {
        /// <summary>Subscribes a handler until the returned lifetime is disposed.</summary>
        /// <param name="handler">The handler invoked for each event.</param>
        /// <returns>The subscription lifetime.</returns>
        IDisposable Subscribe(Action<T> handler);
    }
}
