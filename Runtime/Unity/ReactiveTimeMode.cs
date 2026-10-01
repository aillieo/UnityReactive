// <copyright file="ReactiveTimeMode.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive.Unity
{
    /// <summary>Selects which Unity clock drives a timer stream.</summary>
    public enum ReactiveTimeMode
    {
        /// <summary>Uses <c>Time.deltaTime</c> and pauses when the Unity time scale is zero.</summary>
        Scaled,

        /// <summary>Uses <c>Time.unscaledDeltaTime</c> and continues independently of the Unity time scale.</summary>
        Unscaled,
    }
}
