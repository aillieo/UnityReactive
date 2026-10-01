// <copyright file="ReactiveTimingSample.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive.Samples
{
    using System;
    using AillieoUtils.Reactive.Unity;
    using UnityEngine;

    /// <summary>Shows frame, one-shot timer, and periodic interval streams owned by one scope.</summary>
    public sealed class ReactiveTimingSample : MonoBehaviour
    {
        private ReactiveScope scope;

        /// <summary>Gets the number of Update notifications observed while this component is enabled.</summary>
        public int UpdateCount { get; private set; }

        /// <summary>Gets the most recent zero-based interval value, or -1 before the first tick.</summary>
        public long IntervalTick { get; private set; } = -1;

        /// <summary>Gets a value indicating whether the one-shot unscaled timer has elapsed.</summary>
        public bool TimerElapsed { get; private set; }

        private void OnEnable()
        {
            this.scope = new ReactiveScope();
            this.scope.Add(RxUnity.EveryUpdate.Subscribe(_ => ++this.UpdateCount));
            this.scope.Add(
                RxUnity.Timer(TimeSpan.FromSeconds(1), ReactiveTimeMode.Unscaled)
                    .Subscribe(_ => this.TimerElapsed = true));
            this.scope.Add(
                RxUnity.Interval(TimeSpan.FromSeconds(0.25), ReactiveTimeMode.Unscaled)
                    .Subscribe(value => this.IntervalTick = value));
        }

        private void OnDisable()
        {
            this.scope?.Dispose();
            this.scope = null;
        }
    }
}
