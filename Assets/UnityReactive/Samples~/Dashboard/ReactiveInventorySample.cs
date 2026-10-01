// <copyright file="ReactiveInventorySample.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace AillieoUtils.Reactive.Samples
{
    using AillieoUtils.Reactive.Unity;
    using UnityEngine;

    /// <summary>Attach to an empty GameObject and press Play. Context menu commands work during Play.</summary>
    public sealed class ReactiveInventorySample : MonoBehaviour
    {
        [SerializeField] private int startingGold = 100;
        [SerializeField] private int potionPrice = 15;
        private ReactiveProperty<int> gold;
        private ReactiveDictionary<string, int> inventory;
        private ReactiveScope bindings;

        private void OnEnable()
        {
            this.gold = Rx.Property(this.startingGold);
            this.inventory = Rx.Dictionary<string, int>();
            this.bindings = new ReactiveScope();
            var canBuy = this.bindings.Add(Rx.Computed(() => this.gold.Value >= this.potionPrice));
            var summary = this.bindings.Add(Rx.Computed(() =>
            {
                this.inventory.TryGetValue("Potion", out var potions);
                return $"Gold: {this.gold.Value}; Potions: {potions}; Can buy: {canBuy.Value}";
            }));
            this.bindings.Observe(() => Debug.Log(summary.Value, this));
            this.BuyPotion(); // Both mutations produce one scheduled watch update.
        }

        [ContextMenu("Buy potion (Play Mode)")]
        public void BuyPotion()
        {
            if (!Application.isPlaying || this.gold == null || this.gold.Value < this.potionPrice)
            {
                return;
            }


            Rx.Batch(() =>
            {
                this.gold.Value -= this.potionPrice;
                this.inventory.TryGetValue("Potion", out var count);
                this.inventory["Potion"] = count + 1;
            });
        }

        [ContextMenu("Add gold (Play Mode)")]
        public void AddGold()
        {
            if (Application.isPlaying && this.gold != null)
            {
                this.gold.Value += 50;
            }
        }

        private void OnDisable()
        {
            this.bindings?.Dispose(); this.bindings = null; this.gold = null;
        }

    }
}
