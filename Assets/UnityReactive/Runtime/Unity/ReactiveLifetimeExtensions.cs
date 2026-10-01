// <copyright file="ReactiveLifetimeExtensions.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace AillieoUtils.Reactive.Unity
{
    using System;
    using UnityEngine;

    /// <summary>Provides lifetime helpers for attaching disposable handles to Unity objects.</summary>
    public static class ReactiveLifetimeExtensions
    {
        /// <summary>Disposes the handle when the owning GameObject is destroyed or disabled.</summary>
        /// <typeparam name="T">The disposable handle type.</typeparam>
        /// <param name="handle">The handle to attach.</param>
        /// <param name="owner">The GameObject that owns the handle.</param>
        /// <param name="disposeOnDisable">Whether to dispose the handle when the owner is disabled.</param>
        /// <returns>The supplied handle.</returns>
        public static T AddTo<T>(this T handle, GameObject owner, bool disposeOnDisable = false)
            where T : IDisposable
        {
            if (handle == null)
            {
                throw new ArgumentNullException(nameof(handle));
            }

            if (owner == null)
            {
                handle.Dispose();
                return handle;
            }

            var lifetime = owner.GetComponent<ReactiveLifetime>();
            if (lifetime == null)
            {
                lifetime = owner.AddComponent<ReactiveLifetime>();
            }

            return lifetime.Add(handle, disposeOnDisable);
        }

        /// <summary>Disposes the handle when the owning Component's GameObject is destroyed or disabled.</summary>
        /// <typeparam name="T">The disposable handle type.</typeparam>
        /// <param name="handle">The handle to attach.</param>
        /// <param name="owner">The component whose GameObject owns the handle.</param>
        /// <param name="disposeOnDisable">Whether to dispose the handle when the owner is disabled.</param>
        /// <returns>The supplied handle.</returns>
        public static T AddTo<T>(this T handle, Component owner, bool disposeOnDisable = false)
            where T : IDisposable
        {
            return handle.AddTo(owner == null ? null : owner.gameObject, disposeOnDisable);
        }
    }
}
