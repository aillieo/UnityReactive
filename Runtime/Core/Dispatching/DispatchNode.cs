// <copyright file="DispatchNode.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive
{
    internal abstract class DispatchNode
    {
        internal DispatchNode QueueNext;
        internal bool Queued;

        internal abstract void Invoke();
    }
}
