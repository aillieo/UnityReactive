// <copyright file="ReactiveModelState.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive
{
    using System;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Owns the lazily-created signals used by source-generated reactive model properties.
    /// </summary>
    /// <remarks>This type is intended for generated code. Slots are stable for one generated model version.</remarks>
    public sealed class ReactiveModelState
    {
        private readonly Signal[] signals;
        private readonly object[] adapters;

        /// <summary>Initializes a new instance of the <see cref="ReactiveModelState"/> class.</summary>
        /// <param name="slotCount">The number of generated reactive field slots.</param>
        public ReactiveModelState(int slotCount)
        {
            if (slotCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(slotCount));
            }

            this.signals = new Signal[slotCount];
            this.adapters = new object[slotCount];
        }

        /// <summary>Records a read of a generated property when dependency tracking is active.</summary>
        /// <param name="slot">The generated property slot.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Track(int slot)
        {
            if (Tracking.Current != null)
            {
                this.GetSignal(slot).Track();
            }
        }

        /// <summary>Notifies observers of a changed generated property.</summary>
        /// <param name="slot">The generated property slot.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Notify(int slot)
        {
            this.ValidateSlot(slot);
            this.signals[slot]?.Notify();
        }

        /// <summary>Gets an untyped dependency source for a generated property.</summary>
        /// <param name="slot">The generated property slot.</param>
        /// <returns>A stable source adapter for the slot.</returns>
        public IReactiveSource GetSource(int slot)
        {
            return this.GetSignal(slot);
        }

        /// <summary>Gets a typed read-only property adapter for a generated property.</summary>
        /// <typeparam name="T">The generated property value type.</typeparam>
        /// <param name="slot">The generated property slot.</param>
        /// <param name="getter">Reads the backing field without performing dependency tracking.</param>
        /// <returns>A stable property adapter for the slot.</returns>
        public IReadOnlyReactiveProperty<T> GetProperty<T>(int slot, Func<T> getter)
        {
            if (getter == null)
            {
                throw new ArgumentNullException(nameof(getter));
            }

            this.ValidateSlot(slot);
            var adapter = this.adapters[slot];
            if (adapter == null)
            {
                var property = new PropertyAdapter<T>(this.GetSignal(slot), getter);
                this.adapters[slot] = property;
                return property;
            }

            var typed = adapter as IReadOnlyReactiveProperty<T>;
            if (typed == null)
            {
                throw new InvalidOperationException("The reactive model slot already has an incompatible adapter.");
            }

            return typed;
        }

        private Signal GetSignal(int slot)
        {
            this.ValidateSlot(slot);
            return this.signals[slot] ?? (this.signals[slot] = new Signal());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateSlot(int slot)
        {
            if ((uint)slot >= (uint)this.signals.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(slot));
            }
        }

        private sealed class PropertyAdapter<T> : IReadOnlyReactiveProperty<T>
        {
            private readonly Signal signal;
            private readonly Func<T> getter;

            internal PropertyAdapter(Signal signal, Func<T> getter)
            {
                this.signal = signal;
                this.getter = getter;
            }

            Signal IReactiveSource.OnChanged => this.signal;

            public T Value
            {
                get
                {
                    this.signal.Track();
                    return this.getter();
                }
            }
        }
    }
}
