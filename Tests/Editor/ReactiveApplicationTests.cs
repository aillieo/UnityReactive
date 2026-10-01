// <copyright file="ReactiveApplicationTests.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace AillieoUtils.Reactive.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;

    public sealed class ReactiveApplicationTests
    {
        private ReactiveScope owned;

        [SetUp]
        public void Setup()
        {
            this.owned = new ReactiveScope(null);
        }

        [TearDown]
        public void Cleanup()
        {
            this.owned.Dispose();
        }

        [Test]
        public void ShoppingCartTransactionPublishesOneConsistentTotal()
        {
            var lines = new ReactiveCollection<CartLine>(new[] { new CartLine("potion", 12, 2) });
            var discount = Rx.Property(0);
            var subtotal = this.owned.Add(Rx.Computed(() => lines.Sum(line => line.Price * line.Quantity)));
            var payable = this.owned.Add(Rx.Computed(() => subtotal.Value - discount.Value));
            var rendered = new List<int>();
            this.owned.Observe(() => rendered.Add(payable.Value));

            Rx.Batch(() =>
            {
                lines[0] = new CartLine("potion", 12, 3);
                lines.Add(new CartLine("shield", 50, 1));
                discount.Value = 6;
            });

            CollectionAssert.AreEqual(new[] { 24, 80 }, rendered);
            Assert.AreEqual(86, subtotal.Value);

            lines.RemoveAt(1);
            CollectionAssert.AreEqual(new[] { 24, 80, 30 }, rendered);
        }

        [Test]
        public void SelectedEntityReleasesDependenciesFromPreviousEntity()
        {
            var selectedId = Rx.Property(1);
            var firstHealth = Rx.Property(100);
            var secondHealth = Rx.Property(80);
            var healthById = new ReactiveDictionary<int, ReactiveProperty<int>>
            {
                { 1, firstHealth },
                { 2, secondHealth },
            };
            var selectedHealth = this.owned.Add(Rx.Computed(() =>
                healthById.TryGetValue(selectedId.Value, out var health) ? health.Value : -1));
            var rendered = new List<int>();
            this.owned.Observe(() => rendered.Add(selectedHealth.Value));

            secondHealth.Value = 79;
            selectedId.Value = 2;
            firstHealth.Value = 20;
            secondHealth.Value = 75;
            healthById.Remove(2);
            healthById.Add(2, Rx.Property(60));

            CollectionAssert.AreEqual(new[] { 100, 79, 75, -1, 60 }, rendered);
        }

        [Test]
        public void CraftTransactionKeepsInventoryRecipeAndHistoryInSync()
        {
            var inventory = new ReactiveDictionary<string, int>
            {
                { "wood", 3 },
                { "stone", 2 },
            };
            var unlockedRecipes = new ReactiveSet<string>();
            unlockedRecipes.Add("axe");
            var history = new ReactiveCollection<string>();
            var canCraftAxe = this.owned.Add(Rx.Computed(() =>
                unlockedRecipes.Contains("axe") &&
                inventory.TryGetValue("wood", out var wood) && wood >= 2 &&
                inventory.TryGetValue("stone", out var stone) && stone >= 1));
            var states = new List<bool>();
            this.owned.Observe(() => states.Add(canCraftAxe.Value));

            Rx.Batch(() =>
            {
                inventory["wood"] -= 2;
                inventory["stone"] -= 1;
                history.Add("axe");
            });

            CollectionAssert.AreEqual(new[] { true, false }, states);
            Assert.AreEqual(1, inventory["wood"]);
            Assert.AreEqual(1, inventory["stone"]);
            CollectionAssert.AreEqual(new[] { "axe" }, history);

            Rx.Batch(() =>
            {
                inventory["wood"] = 5;
                unlockedRecipes.Remove("axe");
            });
            CollectionAssert.AreEqual(new[] { true, false }, states);
        }

        [Test]
        public void DeferredUiRenderUsesLatestCollectionSnapshot()
        {
            var scheduler = new DeferredScheduler();
            var products = new ReactiveCollection<string>(new[] { "Potion", "Shield" });
            var query = Rx.Property(string.Empty);
            var descending = Rx.Property(false);
            var visible = this.owned.Add(Rx.Computed(() =>
            {
                IEnumerable<string> result = products.Where(product =>
                    product.IndexOf(query.Value, StringComparison.OrdinalIgnoreCase) >= 0);
                return (descending.Value ? result.OrderByDescending(x => x) : result.OrderBy(x => x)).ToArray();
            }));
            var frames = new List<string>();
            this.owned.Observe(() => frames.Add(string.Join(",", visible.Value)), scheduler);

            products.Add("Sword");
            query.Value = "s";
            descending.Value = true;
            Assert.AreEqual(1, frames.Count);

            scheduler.Flush();
            CollectionAssert.AreEqual(new[] { "Potion,Shield", "Sword,Shield" }, frames);

            products.Remove("Shield");
            this.owned.Clear();
            scheduler.Flush();
            Assert.AreEqual(2, frames.Count);
        }

        private readonly struct CartLine
        {
            internal CartLine(string id, int price, int quantity)
            {
                this.Id = id;
                this.Price = price;
                this.Quantity = quantity;
            }

            internal string Id { get; }

            internal int Price { get; }

            internal int Quantity { get; }
        }
    }
}
