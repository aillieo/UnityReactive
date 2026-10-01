// <copyright file="IReactiveScheduler.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive
{
    using System;

    /// <summary>Defines the scheduling contract for reactive effects.</summary>
    public interface IReactiveScheduler
    {
        /// <summary>Schedules an effect for execution.</summary>
        /// <param name="effect">The effect to schedule.</param>
        void Schedule(Action effect);

        /// <summary>Cancels a previously scheduled effect.</summary>
        /// <param name="effect">The effect to cancel.</param>
        void Cancel(Action effect);
    }
}
