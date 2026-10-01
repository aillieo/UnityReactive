// <copyright file="ReactiveUnityTests.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace AillieoUtils.Reactive.Tests
{
    using System;
    using System.Collections;
    using AillieoUtils.Reactive.Unity;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.TestTools;
    using Object = UnityEngine.Object;

    public sealed class ReactiveUnityTests
    {
        private GameObject host;
        private ReactiveScope owned;

        [SetUp] public void Setup()
        {
            this.host = new GameObject("Reactive Test"); this.owned = new ReactiveScope(); ReactiveFrameDriver.EnsureInstalled();
        }


        [UnityTearDown] public IEnumerator Cleanup()
        {
            this.owned.Dispose(); Object.Destroy(this.host); yield return null;
        }


        [UnityTest] public IEnumerator DefaultExecutorFlushesOnUnityFrame()
        {
            var value = Rx.Property(0); int result = 0;
            this.owned.Observe(() => result = value.Value); value.Value = 42; Assert.AreEqual(0, result);
            yield return null; yield return null; Assert.AreEqual(42, result);
        }

        [UnityTest]
        public IEnumerator ExistingSceneDriverKeepsSchedulingAcrossSceneUnload()
        {
            foreach (var driver in Object.FindObjectsOfType<ReactiveFrameDriver>())
            {
                Object.Destroy(driver.gameObject);
            }

            yield return null;

            var temporaryScene = SceneManager.CreateScene("Reactive Driver Lifetime Test");
            var sceneDriver = new GameObject("Scene Reactive Driver");
            SceneManager.MoveGameObjectToScene(sceneDriver, temporaryScene);
            sceneDriver.AddComponent<ReactiveFrameDriver>();
            ReactiveFrameDriver.EnsureInstalled();

            yield return SceneManager.UnloadSceneAsync(temporaryScene);

            var value = Rx.Property(0);
            int runs = 0;
            var subscription = Rx.Observe(() => { _ = value.Value; ++runs; });
            try
            {
                value.Value = 1;
                yield return null;
                Assert.AreEqual(2, runs);
            }
            finally
            {
                subscription.Dispose();
                ReactiveFrameDriver.EnsureInstalled();
            }
        }

        [UnityTest] public IEnumerator DestroyCancelsPendingObserve()
        {
            var value = Rx.Property(0); int runs = 0;
            Rx.Observe(() => { _ = value.Value; ++runs; }).AddTo(this.host);
            Object.Destroy(this.host); yield return null; yield return null;
            int before = runs; value.Value = 1; Rx.FrameScheduler.Flush(); Assert.AreEqual(before, runs);
            Assert.AreEqual(0, value.OnChanged.ListenerCount);
        }

        [Test] public void DisableDisposesImmediatelyAndCancelsQueuedWork()
        {
            var value = Rx.Property(0); int runs = 0;
            Rx.Observe(() => { _ = value.Value; ++runs; }).AddTo(this.host, true);
            value.Value = 1; this.host.SetActive(false); Rx.FrameScheduler.Flush(); Assert.AreEqual(1, runs);
            this.host.SetActive(true); value.Value = 2; Rx.FrameScheduler.Flush(); Assert.AreEqual(1, runs);
        }

        [Test] public void DisableLifetimeAcceptsNewBindingsAfterReenable()
        {
            var value = Rx.Property(0); int firstRuns = 0; int secondRuns = 0;
            Rx.Observe(() => { _ = value.Value; ++firstRuns; }).AddTo(this.host, true);
            this.host.SetActive(false);
            this.host.SetActive(true);
            Rx.Observe(() => { _ = value.Value; ++secondRuns; }).AddTo(this.host, true);

            value.Value = 1; Rx.FrameScheduler.Flush();

            Assert.AreEqual(1, firstRuns);
            Assert.AreEqual(2, secondRuns);
        }

        [Test] public void TwoWayToggleDoesNotEmitFeedbackAndUnbinds()
        {
            var toggle = this.host.AddComponent<UnityEngine.UI.Toggle>(); var model = Rx.Property(false); int events = 0;
            toggle.onValueChanged.AddListener(_ => ++events);
            var binding = this.owned.BindToggleTwoWay(toggle, model);
            Assert.AreEqual(1, this.owned.Count);
            model.Value = true; Rx.FrameScheduler.Flush();
            Assert.IsTrue(toggle.isOn); Assert.AreEqual(0, events);
            toggle.isOn = false; Assert.IsFalse(model.Value); Assert.AreEqual(1, events);
            binding.Dispose(); toggle.isOn = true; Assert.IsFalse(model.Value);
        }

        [Test] public void TwoWaySliderDoesNotEmitFeedback()
        {
            var slider = this.host.AddComponent<UnityEngine.UI.Slider>(); var model = Rx.Property(0.25f); int events = 0;
            slider.onValueChanged.AddListener(_ => ++events); this.owned.BindSliderTwoWay(slider, model);
            Assert.AreEqual(1, this.owned.Count);
            model.Value = 0.75f; Rx.FrameScheduler.Flush(); Assert.AreEqual(0.75f, slider.value); Assert.AreEqual(0, events);
            slider.value = 0.5f; Assert.AreEqual(0.5f, model.Value);
        }

        [Test] public void CollectionRendererDoesNotBecomeDependency()
        {
            var values = Rx.Collection<int>(); var unrelated = Rx.Property(0); int renders = 0;
            this.owned.BindCollection(values, snapshot => { _ = unrelated.Value; ++renders; });
            unrelated.Value++; Rx.FrameScheduler.Flush(); Assert.AreEqual(1, renders);
            values.Add(1); Rx.FrameScheduler.Flush(); Assert.AreEqual(2, renders);
        }

        [UnityTest]
        public IEnumerator EveryUpdateEmitsUnitAndStopsAfterDisposal()
        {
            int calls = 0;
            Unit latest = default;
            var subscription = RxUnity.EveryUpdate.Subscribe(value =>
            {
                ++calls;
                latest = value;
            });

            yield return null;

            Assert.GreaterOrEqual(calls, 1);
            Assert.AreEqual(Unit.Default, latest);

            subscription.Dispose();
            int callsAfterDisposal = calls;
            yield return null;

            Assert.AreEqual(callsAfterDisposal, calls);
        }

        [UnityTest]
        public IEnumerator UnscaledTimerContinuesWhileScaledTimerIsPaused()
        {
            float previousTimeScale = Time.timeScale;
            int scaledCalls = 0;
            int unscaledCalls = 0;
            try
            {
                Time.timeScale = 0f;
                this.owned.Add(
                    RxUnity.Timer(TimeSpan.FromMilliseconds(50), ReactiveTimeMode.Scaled)
                        .Subscribe(_ => ++scaledCalls));
                this.owned.Add(
                    RxUnity.Timer(TimeSpan.FromMilliseconds(50), ReactiveTimeMode.Unscaled)
                        .Subscribe(_ => ++unscaledCalls));

                yield return new WaitForSecondsRealtime(0.15f);

                Assert.AreEqual(0, scaledCalls);
                Assert.AreEqual(1, unscaledCalls);
            }
            finally
            {
                Time.timeScale = previousTimeScale;
            }
        }

        [UnityTest]
        public IEnumerator IntervalEmitsIncreasingValuesAndStopsAfterDisposal()
        {
            long latest = -1;
            int calls = 0;
            var subscription = RxUnity.Interval(
                    TimeSpan.Zero,
                    TimeSpan.FromMilliseconds(1),
                    ReactiveTimeMode.Unscaled)
                .Subscribe(value =>
                {
                    latest = value;
                    ++calls;
                });

            yield return null;

            Assert.GreaterOrEqual(calls, 1);
            Assert.AreEqual(calls - 1, latest);

            subscription.Dispose();
            int callsAfterDisposal = calls;
            yield return null;

            Assert.AreEqual(callsAfterDisposal, calls);
        }

    }
}
