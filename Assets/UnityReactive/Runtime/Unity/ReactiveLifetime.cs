// <copyright file="ReactiveLifetime.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace AillieoUtils.Reactive.Unity
{
    using System;
    using UnityEngine;

    /// <summary>Owns disposable handles for the lifetime of a GameObject.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public sealed class ReactiveLifetime : MonoBehaviour
    {
        private readonly CompositeDisposable destroy = new CompositeDisposable();
        private readonly CompositeDisposable disable = new CompositeDisposable();
        private bool destroyed;

        /// <summary>Adds a disposable handle and optionally disposes it when the component is disabled.</summary>
        /// <typeparam name="T">The disposable handle type.</typeparam>
        /// <param name="handle">The handle to own.</param>
        /// <param name="onDisable">Whether to dispose the handle when this component is disabled.</param>
        /// <returns>The supplied handle.</returns>
        public T Add<T>(T handle, bool onDisable = false)
            where T : IDisposable
        {
            if (this.destroyed || (onDisable && !this.isActiveAndEnabled))
            {
                handle.Dispose();
                return handle;
            }

            return (onDisable ? this.disable : this.destroy).Add(handle);
        }

        private void OnDisable()
        {
            this.Clear(this.disable);
        }

        private void OnDestroy()
        {
            this.destroyed = true;
            this.Dispose(this.disable);
            this.Dispose(this.destroy);
        }

        private void Clear(CompositeDisposable disposables)
        {
            try
            {
                disposables.Clear();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void Dispose(CompositeDisposable disposables)
        {
            try
            {
                disposables.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
