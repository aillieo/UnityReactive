// <copyright file="GeneratedReactiveModelSample.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive.Samples
{
    using UnityEngine;

    /// <summary>Shows field-to-property generation without a model base class or runtime reflection.</summary>
    public sealed class GeneratedReactiveModelSample : MonoBehaviour
    {
        private ReactiveScope scope;

        public GeneratedPlayerModel Model { get; } = new GeneratedPlayerModel();

        public string Status { get; private set; }

        public int RenderCount { get; private set; }

        private void OnEnable()
        {
            this.scope = new ReactiveScope();
            this.scope.Observe(() =>
            {
                this.Status = $"{this.Model.DisplayName}: {this.Model.Health} HP / {this.Model.Gold} gold";
                ++this.RenderCount;
            });
        }

        public void TakeDamage(int amount)
        {
            this.Model.Health = Mathf.Max(0, this.Model.Health - amount);
        }

        /// <summary>Applies several externally supplied values as one reactive update.</summary>
        public void ApplySnapshot(int health, int gold, string displayName)
        {
            Rx.Batch(() =>
            {
                this.Model.Health = health;
                this.Model.Gold = gold;
                this.Model.DisplayName = displayName;
            });
            this.Model.RecordSnapshot();
        }

        private void OnDisable()
        {
            this.scope?.Dispose();
            this.scope = null;
        }
    }

    /// <summary>A plain model whose reactive implementation is supplied at compile time.</summary>
    [ReactiveModel]
    public sealed partial class GeneratedPlayerModel
    {
        private int health = 100;

        private int gold = 25;

        [Reactive("DisplayName")]
        private string playerName = "Player";

        [NonReactive]
        private int snapshotCount;

        public int HealthChanges { get; private set; }

        public int SnapshotCount => this.snapshotCount;

        internal void RecordSnapshot()
        {
            ++this.snapshotCount;
        }

        partial void OnHealthChanged(int oldValue, int newValue)
        {
            ++this.HealthChanges;
        }
    }
}
