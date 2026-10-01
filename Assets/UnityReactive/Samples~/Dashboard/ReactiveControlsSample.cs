// <copyright file="ReactiveControlsSample.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace AillieoUtils.Reactive.Samples
{
    using AillieoUtils.Reactive.Unity;
    using UnityEngine;

    /// <summary>Optional Inspector-wired uGUI example. Toggle and slider update the model in both directions.</summary>
    public sealed class ReactiveControlsSample : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Toggle soundToggle;
        [SerializeField] private UnityEngine.UI.Slider volumeSlider;
        [SerializeField] private UnityEngine.UI.Text statusText;
        private readonly ReactiveProperty<bool> sound = Rx.Property(true);
        private readonly ReactiveProperty<float> volume = Rx.Property(0.5f);
        private ReactiveScope bindings;

        private void OnEnable()
        {
            this.bindings = new ReactiveScope();
            if (this.soundToggle != null)
            {
                this.bindings.BindToggleTwoWay(this.soundToggle, this.sound);
            }

            if (this.volumeSlider != null)
            {
                this.bindings.BindSliderTwoWay(this.volumeSlider, this.volume);
            }

            var label = this.bindings.Add(Rx.Computed(() => $"Sound: {(this.sound.Value ? "On" : "Off")}  Volume: {this.volume.Value:P0}"));
            if (this.statusText != null)
            {
                this.bindings.BindText(this.statusText, label);
            }
        }

        private void OnDisable()
        {
            this.bindings?.Dispose(); this.bindings = null;
        }

    }
}
