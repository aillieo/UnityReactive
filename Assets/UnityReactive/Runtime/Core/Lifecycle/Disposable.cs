// <copyright file="Disposable.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive
{
    using System;

    /// <summary>Creates a disposable wrapper around a cleanup callback.</summary>
    public sealed class Disposable : IDisposable
    {
        private Action action;

        private Disposable(Action action)
        {
            this.action = action ?? throw new ArgumentNullException(nameof(action));
        }

        /// <summary>Creates a disposable that invokes an action at most once.</summary>
        /// <param name="action">The cleanup action.</param>
        /// <returns>A disposable wrapper for the action.</returns>
        public static Disposable Create(Action action)
        {
            return new Disposable(action);
        }

        /// <summary>Invokes the cleanup action once and releases it.</summary>
        public void Dispose()
        {
            var callback = this.action;
            this.action = null;
            callback?.Invoke();
        }
    }
}
