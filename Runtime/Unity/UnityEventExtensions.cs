// <copyright file="UnityEventExtensions.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace AillieoUtils.Reactive.Unity
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.UI;

    /// <summary>Converts UnityEvent instances into disposable subscriptions.</summary>
    public static class UnityEventExtensions
    {
        /// <summary>Subscribes to a parameterless UnityEvent.</summary>
        /// <param name="unityEvent">The event to observe.</param>
        /// <param name="callback">The callback invoked by the event.</param>
        /// <returns>A handle that removes the listener when disposed.</returns>
        public static IDisposable Subscribe(this UnityEvent unityEvent, Action callback)
        {
            var unityAction = new UnityAction(callback);
            unityEvent.AddListener(unityAction);
            return Disposable.Create(() => unityEvent.RemoveListener(unityAction));
        }

        /// <summary>Subscribes to a single-argument UnityEvent.</summary>
        /// <typeparam name="T">The event argument type.</typeparam>
        /// <param name="unityEvent">The event to observe.</param>
        /// <param name="callback">The callback invoked by the event.</param>
        /// <returns>A handle that removes the listener when disposed.</returns>
        public static IDisposable Subscribe<T>(this UnityEvent<T> unityEvent, Action<T> callback)
        {
            var unityAction = new UnityAction<T>(callback);
            unityEvent.AddListener(unityAction);
            return Disposable.Create(() => unityEvent.RemoveListener(unityAction));
        }

        /// <summary>Subscribes to a two-argument UnityEvent.</summary>
        /// <typeparam name="T1">The first event argument type.</typeparam>
        /// <typeparam name="T2">The second event argument type.</typeparam>
        /// <param name="unityEvent">The event to observe.</param>
        /// <param name="callback">The callback invoked by the event.</param>
        /// <returns>A handle that removes the listener when disposed.</returns>
        public static IDisposable Subscribe<T1, T2>(this UnityEvent<T1, T2> unityEvent, Action<T1, T2> callback)
        {
            var unityAction = new UnityAction<T1, T2>(callback);
            unityEvent.AddListener(unityAction);
            return Disposable.Create(() => unityEvent.RemoveListener(unityAction));
        }

        /// <summary>Subscribes to a three-argument UnityEvent.</summary>
        /// <typeparam name="T1">The first event argument type.</typeparam>
        /// <typeparam name="T2">The second event argument type.</typeparam>
        /// <typeparam name="T3">The third event argument type.</typeparam>
        /// <param name="unityEvent">The event to observe.</param>
        /// <param name="callback">The callback invoked by the event.</param>
        /// <returns>A handle that removes the listener when disposed.</returns>
        public static IDisposable Subscribe<T1, T2, T3>(this UnityEvent<T1, T2, T3> unityEvent, Action<T1, T2, T3> callback)
        {
            var unityAction = new UnityAction<T1, T2, T3>(callback);
            unityEvent.AddListener(unityAction);
            return Disposable.Create(() => unityEvent.RemoveListener(unityAction));
        }

        /// <summary>Subscribes to a four-argument UnityEvent.</summary>
        /// <typeparam name="T1">The first event argument type.</typeparam>
        /// <typeparam name="T2">The second event argument type.</typeparam>
        /// <typeparam name="T3">The third event argument type.</typeparam>
        /// <typeparam name="T4">The fourth event argument type.</typeparam>
        /// <param name="unityEvent">The event to observe.</param>
        /// <param name="callback">The callback invoked by the event.</param>
        /// <returns>A handle that removes the listener when disposed.</returns>
        public static IDisposable Subscribe<T1, T2, T3, T4>(this UnityEvent<T1, T2, T3, T4> unityEvent, Action<T1, T2, T3, T4> callback)
        {
            var unityAction = new UnityAction<T1, T2, T3, T4>(callback);
            unityEvent.AddListener(unityAction);
            return Disposable.Create(() => unityEvent.RemoveListener(unityAction));
        }
    }
}
