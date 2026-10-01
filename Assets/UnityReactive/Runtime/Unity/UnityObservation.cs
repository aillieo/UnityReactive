// <copyright file="UnityObservation.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace AillieoUtils.Reactive.Unity
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>Provides disposable observations for common Unity UI events.</summary>
    public static class UnityObservation
    {
        /// <summary>Subscribes to a button click event.</summary>
        /// <param name="button">The button to observe.</param>
        /// <param name="onClick">The callback invoked when the button is clicked.</param>
        /// <returns>A handle that removes the listener when disposed.</returns>
        public static IDisposable ObserveOnClick(this Button button, Action onClick)
        {
            return button.onClick.Subscribe(onClick);
        }

        /// <summary>Subscribes to toggle value changes.</summary>
        /// <param name="toggle">The toggle to observe.</param>
        /// <param name="onValueChanged">The callback that receives new toggle values.</param>
        /// <returns>A handle that removes the listener when disposed.</returns>
        public static IDisposable ObserveOnValueChanged(this Toggle toggle, Action<bool> onValueChanged)
        {
            return toggle.onValueChanged.Subscribe(onValueChanged);
        }

        /// <summary>Subscribes to slider value changes.</summary>
        /// <param name="slider">The slider to observe.</param>
        /// <param name="onValueChanged">The callback that receives new slider values.</param>
        /// <returns>A handle that removes the listener when disposed.</returns>
        public static IDisposable ObserveOnValueChanged(this Slider slider, Action<float> onValueChanged)
        {
            return slider.onValueChanged.Subscribe(onValueChanged);
        }

        /// <summary>Subscribes to input field value changes.</summary>
        /// <param name="inputField">The input field to observe.</param>
        /// <param name="onValueChanged">The callback that receives new text values.</param>
        /// <returns>A handle that removes the listener when disposed.</returns>
        public static IDisposable ObserveOnValueChanged(this InputField inputField, Action<string> onValueChanged)
        {
            return inputField.onValueChanged.Subscribe(onValueChanged);
        }

        /// <summary>Subscribes to dropdown value changes.</summary>
        /// <param name="dropdown">The dropdown to observe.</param>
        /// <param name="onValueChanged">The callback that receives new selected indices.</param>
        /// <returns>A handle that removes the listener when disposed.</returns>
        public static IDisposable ObserveOnValueChanged(this Dropdown dropdown, Action<int> onValueChanged)
        {
            return dropdown.onValueChanged.Subscribe(onValueChanged);
        }

        /// <summary>Subscribes to scroll view position changes.</summary>
        /// <param name="scrollRect">The scroll view to observe.</param>
        /// <param name="onValueChanged">The callback that receives new normalized positions.</param>
        /// <returns>A handle that removes the listener when disposed.</returns>
        public static IDisposable ObserveOnValueChanged(this ScrollRect scrollRect, Action<Vector2> onValueChanged)
        {
            return scrollRect.onValueChanged.Subscribe(onValueChanged);
        }
    }
}
