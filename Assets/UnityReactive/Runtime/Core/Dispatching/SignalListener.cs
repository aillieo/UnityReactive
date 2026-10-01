// <copyright file="SignalListener.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive
{
    using System;

    internal abstract class SignalListener : IDisposable
    {
        internal Signal Owner;
        internal bool Priority;
        internal SignalListener Previous;
        internal SignalListener Next;
        internal long DeliveredVersion;

        public virtual void Dispose()
        {
            this.Owner?.Detach(this);
        }

        internal abstract void OnSignal();

        internal virtual void OnDetached()
        {
        }
    }
}
