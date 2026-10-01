// <copyright file="UnityBindings.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace AillieoUtils.Reactive.Unity
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>Provides one-way and two-way bindings between reactive values and Unity UI objects.</summary>
    public static class UnityBindings
    {
        /// <summary>Binds a GameObject active state to a reactive boolean.</summary>
        /// <param name="group">The scope that owns the binding.</param>
        /// <param name="target">The GameObject to update.</param>
        /// <param name="value">The reactive active state.</param>
        /// <returns>The binding lifetime.</returns>
        public static IDisposable BindActive(this ReactiveScope group, GameObject target, IReadOnlyReactiveProperty<bool> value)
        {
            return group.Observe(
                () =>
                {
                    if (target != null)
                    {
                        target.SetActive(value.Value);
                    }
                }, value);
        }

        /// <summary>Binds a Behaviour enabled state to a reactive boolean.</summary>
        /// <param name="group">The scope that owns the binding.</param>
        /// <param name="target">The behaviour to update.</param>
        /// <param name="value">The reactive enabled state.</param>
        /// <returns>The binding lifetime.</returns>
        public static IDisposable BindEnabled(this ReactiveScope group, Behaviour target, IReadOnlyReactiveProperty<bool> value)
        {
            return group.Observe(
                () =>
                {
                    if (target != null)
                    {
                        target.enabled = value.Value;
                    }
                }, value);
        }

        /// <summary>Binds a Selectable interactable state to a reactive boolean.</summary>
        /// <param name="group">The scope that owns the binding.</param>
        /// <param name="target">The selectable to update.</param>
        /// <param name="value">The reactive interactable state.</param>
        /// <returns>The binding lifetime.</returns>
        public static IDisposable BindInteractable(this ReactiveScope group, UnityEngine.UI.Selectable target, IReadOnlyReactiveProperty<bool> value)
        {
            return group.Observe(
                () =>
                {
                    if (target != null)
                    {
                        target.interactable = value.Value;
                    }
                }, value);
        }

        /// <summary>Binds a Text component to a reactive string.</summary>
        /// <param name="group">The scope that owns the binding.</param>
        /// <param name="target">The text component to update.</param>
        /// <param name="value">The reactive text value.</param>
        /// <returns>The binding lifetime.</returns>
        public static IDisposable BindText(this ReactiveScope group, UnityEngine.UI.Text target, IReadOnlyReactiveProperty<string> value)
        {
            return group.Observe(
                () =>
                {
                    if (target != null)
                    {
                        target.text = value.Value;
                    }
                }, value);
        }

        /// <summary>Binds a Toggle value without raising a feedback event.</summary>
        /// <param name="group">The scope that owns the binding.</param>
        /// <param name="target">The toggle to update.</param>
        /// <param name="value">The reactive toggle value.</param>
        /// <returns>The binding lifetime.</returns>
        public static IDisposable BindToggle(this ReactiveScope group, UnityEngine.UI.Toggle target, IReadOnlyReactiveProperty<bool> value)
        {
            return group.Observe(
                () =>
                {
                    if (target != null)
                    {
                        target.SetIsOnWithoutNotify(value.Value);
                    }
                }, value);
        }

        /// <summary>Binds a Slider value without raising a feedback event.</summary>
        /// <param name="group">The scope that owns the binding.</param>
        /// <param name="target">The slider to update.</param>
        /// <param name="value">The reactive slider value.</param>
        /// <returns>The binding lifetime.</returns>
        public static IDisposable BindSlider(this ReactiveScope group, UnityEngine.UI.Slider target, IReadOnlyReactiveProperty<float> value)
        {
            return group.Observe(
                () =>
                {
                    if (target != null)
                    {
                        target.SetValueWithoutNotify(value.Value);
                    }
                }, value);
        }

        /// <summary>Binds a Dropdown value without raising a feedback event.</summary>
        /// <param name="group">The scope that owns the binding.</param>
        /// <param name="target">The dropdown to update.</param>
        /// <param name="value">The reactive selected index.</param>
        /// <returns>The binding lifetime.</returns>
        public static IDisposable BindDropdown(this ReactiveScope group, UnityEngine.UI.Dropdown target, IReadOnlyReactiveProperty<int> value)
        {
            return group.Observe(
                () =>
                {
                    if (target != null)
                    {
                        target.SetValueWithoutNotify(value.Value);
                    }
                }, value);
        }

        /// <summary>Binds a RawImage texture to a reactive texture.</summary>
        /// <param name="group">The scope that owns the binding.</param>
        /// <param name="target">The raw image to update.</param>
        /// <param name="value">The reactive texture.</param>
        /// <returns>The binding lifetime.</returns>
        public static IDisposable BindTexture(this ReactiveScope group, UnityEngine.UI.RawImage target, IReadOnlyReactiveProperty<Texture> value)
        {
            return group.Observe(
                () =>
                {
                    if (target != null)
                    {
                        target.texture = value.Value;
                    }
                }, value);
        }

        /// <summary>Binds an Image sprite to a reactive sprite.</summary>
        /// <param name="group">The scope that owns the binding.</param>
        /// <param name="target">The image to update.</param>
        /// <param name="value">The reactive sprite.</param>
        /// <returns>The binding lifetime.</returns>
        public static IDisposable BindSprite(this ReactiveScope group, UnityEngine.UI.Image target, IReadOnlyReactiveProperty<Sprite> value)
        {
            return group.Observe(
                () =>
                {
                    if (target != null)
                    {
                        target.sprite = value.Value;
                    }
                }, value);
        }

        /// <summary>Creates a two-way binding for a Toggle and reactive boolean.</summary>
        /// <param name="group">The scope that owns the binding.</param>
        /// <param name="target">The toggle to synchronize.</param>
        /// <param name="value">The mutable reactive toggle value.</param>
        /// <returns>The binding lifetime.</returns>
        public static IDisposable BindToggleTwoWay(this ReactiveScope group, UnityEngine.UI.Toggle target, IReactiveProperty<bool> value)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            var binding = new CompositeDisposable();
            try
            {
                binding.Add(group.CreateObservation(
                    () =>
                    {
                        if (target != null)
                        {
                            target.SetIsOnWithoutNotify(value.Value);
                        }
                    }, value));
                UnityEngine.Events.UnityAction<bool> action = next => value.Value = next;
                target.onValueChanged.AddListener(action);
                binding.Add(Disposable.Create(() =>
                {
                    if (target != null)
                    {
                        target.onValueChanged.RemoveListener(action);
                    }
                }));
                return group.Add(binding);
            }
            catch
            {
                binding.Dispose();
                throw;
            }
        }

        /// <summary>Creates a two-way binding for a Slider and reactive float.</summary>
        /// <param name="group">The scope that owns the binding.</param>
        /// <param name="target">The slider to synchronize.</param>
        /// <param name="value">The mutable reactive slider value.</param>
        /// <returns>The binding lifetime.</returns>
        public static IDisposable BindSliderTwoWay(this ReactiveScope group, UnityEngine.UI.Slider target, IReactiveProperty<float> value)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            var binding = new CompositeDisposable();
            try
            {
                binding.Add(group.CreateObservation(
                    () =>
                    {
                        if (target != null)
                        {
                            target.SetValueWithoutNotify(value.Value);
                        }
                    }, value));
                UnityEngine.Events.UnityAction<float> action = next => value.Value = next;
                target.onValueChanged.AddListener(action);
                binding.Add(Disposable.Create(() =>
                {
                    if (target != null)
                    {
                        target.onValueChanged.RemoveListener(action);
                    }
                }));
                return group.Add(binding);
            }
            catch
            {
                binding.Dispose();
                throw;
            }
        }

        /// <summary>Adapter for a project's list view/pool. Rendering executes outside dependency tracking.</summary>
        /// <typeparam name="T">The item type rendered by the adapter.</typeparam>
        /// <param name="group">The scope that owns the binding.</param>
        /// <param name="values">The values to snapshot whenever their reactive dependencies change.</param>
        /// <param name="render">The callback that renders each snapshot.</param>
        /// <returns>The binding lifetime.</returns>
        public static IDisposable BindCollection<T>(this ReactiveScope group, IEnumerable<T> values, Action<IReadOnlyList<T>> render)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (render == null)
            {
                throw new ArgumentNullException(nameof(render));
            }

            return group.Observe(() => (IReadOnlyList<T>)new List<T>(values), render);
        }
    }
}
