// <copyright file="ReactiveSampleTests.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace AillieoUtils.Reactive.Samples.Tests
{
    using System.Collections;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;

    public sealed class ReactiveSampleTests
    {
        private GameObject host;

        [SetUp]
        public void Setup()
        {
            this.host = new GameObject("Reactive Sample Test");
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Object.Destroy(this.host);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DashboardBatchesStateAndRebindsAfterEnable()
        {
            var dashboard = this.host.AddComponent<ReactiveDashboardSample>();
            yield return null;
            int before = dashboard.RenderCount;
            dashboard.ApplyBatch();
            Rx.FrameScheduler.Flush();
            Assert.AreEqual(before + 1, dashboard.RenderCount);
            Assert.AreEqual(90, dashboard.Health.Value);
            Assert.AreEqual(125, dashboard.Gold.Value);
            dashboard.BuyPotion();
            Rx.FrameScheduler.Flush();
            Assert.AreEqual(1, dashboard.Inventory.Count);
            Assert.AreEqual(110, dashboard.Gold.Value);
            dashboard.Health.Value = 0;
            Rx.FrameScheduler.Flush();
            StringAssert.Contains("DEFEATED", dashboard.Status);
            dashboard.enabled = false;
            before = dashboard.RenderCount;
            dashboard.Health.Value = 50;
            Rx.FrameScheduler.Flush();
            Assert.AreEqual(before, dashboard.RenderCount);
            dashboard.enabled = true;
            StringAssert.Contains("READY", dashboard.Status);
            Assert.AreEqual(1, this.host.GetComponentsInChildren<Canvas>().Length);
        }

        [UnityTest]
        public IEnumerator InventorySampleRunsAndReleasesOnDisable()
        {
            var sample = this.host.AddComponent<ReactiveInventorySample>();
            sample.BuyPotion();
            yield return null;
            yield return null;
            sample.enabled = false;
            yield return null;
            Assert.AreEqual(0, Rx.FrameScheduler.PendingCount);
        }

        [UnityTest]
        public IEnumerator GeneratedModelSampleTracksFieldsAndBatchesSnapshots()
        {
            var sample = this.host.AddComponent<GeneratedReactiveModelSample>();
            yield return null;
            Assert.AreEqual("Player: 100 HP / 25 gold", sample.Status);
            int before = sample.RenderCount;

            sample.ApplySnapshot(80, 125, "Aillie");
            Rx.FrameScheduler.Flush();

            Assert.AreEqual(before + 1, sample.RenderCount);
            Assert.AreEqual("Aillie: 80 HP / 125 gold", sample.Status);
            Assert.AreEqual(1, sample.Model.HealthChanges);
            Assert.AreEqual(1, sample.Model.SnapshotCount);
            Assert.AreSame(sample.Model.HealthProperty, sample.Model.HealthProperty);
        }

        [UnityTest]
        public IEnumerator TimingSampleOwnsAndReleasesFrameSubscription()
        {
            var sample = this.host.AddComponent<ReactiveTimingSample>();

            yield return null;

            Assert.GreaterOrEqual(sample.UpdateCount, 1);
            sample.enabled = false;
            int updatesAfterDisable = sample.UpdateCount;

            yield return null;

            Assert.AreEqual(updatesAfterDisable, sample.UpdateCount);
        }
    }
}
