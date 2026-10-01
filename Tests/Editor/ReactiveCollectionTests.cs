// <copyright file="ReactiveCollectionTests.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace AillieoUtils.Reactive.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;

    public sealed class ReactiveCollectionTests
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
        public void ListTracksCountSeparatelyFromIndividualIndices()
        {
            var list = new ReactiveCollection<int>(new[] { 1, 2 }); int countRuns = 0, indexRuns = 0;
            this.owned.Observe(() => { _ = list.Count; ++countRuns; });
            this.owned.Observe(() => { _ = list[0]; ++indexRuns; });
            list[1] = 3; Assert.AreEqual(1, countRuns); Assert.AreEqual(1, indexRuns);
            list[0] = 4; Assert.AreEqual(2, indexRuns); list.Add(5);
            Assert.AreEqual(2, countRuns); Assert.AreEqual(2, indexRuns);
        }

        [Test]
        public void ListInsertionRemovalAndClearInvalidateShiftedIndices()
        {
            var list = new ReactiveCollection<int>(new[] { 1, 2, 3 }); int result = 0;
            this.owned.Observe(() => { list.TryGetValue(1, out result); });
            list.Insert(0, 9); Assert.AreEqual(1, result);
            list.RemoveAt(0); Assert.AreEqual(2, result);
            list.Clear(); Assert.AreEqual(0, result);
            list.AddRange(new[] { 4, 5 }); Assert.AreEqual(5, result);
        }

        [Test]
        public void MissingListIndexBecomesTrackedWhenAdded()
        {
            var list = Rx.Collection<int>(); bool found = false;
            this.owned.Observe(() => found = list.TryGetValue(2, out _));
            list.AddRange(new[] { 1, 2, 3 }); Assert.IsTrue(found);
        }

        [Test]
        public void RxCollectionFactoriesPreserveFineGrainedTrackingForBothCachePolicies()
        {
            var policies = new[] { KeySignalCachePolicy.Weak, KeySignalCachePolicy.Strong };
            foreach (var policy in policies)
            {
                var list = Rx.Collection<int>(policy);
                var map = Rx.Dictionary<string, int>(policy);
                var set = Rx.Set<string>(policy);
                int listValue = 0;
                int mapValue = 0;
                bool setValue = false;

                list.Add(1);
                this.owned.Observe(() => listValue = list[0]);
                this.owned.Observe(() => map.TryGetValue("tracked", out mapValue));
                this.owned.Observe(() => setValue = set.Contains("tracked"));

                list[0] = 2;
                map["other"] = 1;
                map["tracked"] = 3;
                set.Add("other");
                set.Add("tracked");

                Assert.AreEqual(2, listValue, policy.ToString());
                Assert.AreEqual(3, mapValue, policy.ToString());
                Assert.IsTrue(setValue, policy.ToString());
            }
        }

        [Test]
        public void ListEnumerationTracksValuesAndStructure()
        {
            var list = new ReactiveCollection<int>(new[] { 1, 2 }); int sum = 0;
            this.owned.Observe(() => sum = list.Sum()); list[1] = 5; Assert.AreEqual(6, sum);
            list.Add(3); Assert.AreEqual(9, sum);
        }

        [Test]
        public void SpliceZeroDeleteInsertsWithoutRemovingTail()
        {
            var list = new ReactiveCollection<int>(new[] { 1, 2, 3 }); int runs = 0;
            this.owned.Observe(() => { _ = list.ToArray(); ++runs; });
            Assert.IsEmpty(list.Splice(1, 0, 9)); CollectionAssert.AreEqual(new[] { 1, 9, 2, 3 }, list);
            Assert.AreEqual(2, runs); CollectionAssert.AreEqual(new[] { 2 }, list.Splice(-2, 1));
            CollectionAssert.AreEqual(new[] { 1, 9, 3 }, list);
        }

        [Test]
        public void SortAndReverseNotifyValuesWithoutChangingCount()
        {
            var list = new ReactiveCollection<int>(new[] { 3, 1, 2 }); int first = 0, countRuns = 0;
            this.owned.Observe(() => first = list[0]); this.owned.Observe(() => { _ = list.Count; ++countRuns; });
            list.Sort(); Assert.AreEqual(1, first); list.Reverse(); Assert.AreEqual(3, first); Assert.AreEqual(1, countRuns);
        }

        [Test]
        public void SortAndReverseReorderValuesThatAreEqualForReactiveNotifications()
        {
            var sorted = new ReactiveCollection<int>(new[] { 3, 1 }, new ParityComparer());
            var reversed = new ReactiveCollection<int>(new[] { 1, 3 }, new ParityComparer());

            sorted.Sort();
            reversed.Reverse();

            CollectionAssert.AreEqual(new[] { 1, 3, 3, 1 }, sorted.Concat(reversed));
        }

        [Test]
        public void ListStandardInterfaceAndSelfAddRangeWork()
        {
            var list = new ReactiveCollection<int>(new[] { 1, 2 }); IList<int> api = list;
            Assert.IsTrue(api.Contains(2)); Assert.AreEqual(0, api.IndexOf(1));
            list.AddRange(list); CollectionAssert.AreEqual(new[] { 1, 2, 1, 2 }, list);
            Assert.IsTrue(api.Remove(1)); Assert.IsFalse(api.Remove(8));
            var copy = new int[3]; api.CopyTo(copy, 0); CollectionAssert.AreEqual(new[] { 2, 1, 2 }, copy);
        }

        [Test]
        public void MapTracksMissingKeysAndIgnoresOtherKeys()
        {
            var map = Rx.Dictionary<string, int>(); int runs = 0, result = -1;
            this.owned.Observe(() => { map.TryGetValue("a", out result); ++runs; });
            map["b"] = 1; Assert.AreEqual(1, runs);
            map["a"] = 0; Assert.AreEqual(2, runs); // Missing -> present default value is a change.
            map["a"] = 0; Assert.AreEqual(2, runs); map["a"] = 3; Assert.AreEqual(3, result);
            map.Remove("a"); Assert.AreEqual(0, result);
        }

        [Test]
        public void MapCountAndKeysDoNotChangeWhenReplacingValue()
        {
            var map = Rx.Dictionary<string, string>(); map["a"] = null; int count = 0, keys = 0, values = 0;
            this.owned.Observe(() => { _ = map.Count; ++count; });
            this.owned.Observe(() => { _ = map.Keys; ++keys; });
            this.owned.Observe(() => { _ = map.Values; ++values; });
            map["a"] = "x"; Assert.AreEqual(1, count); Assert.AreEqual(1, keys); Assert.AreEqual(2, values);
            map["b"] = null; Assert.AreEqual(2, count);
        }

        [Test]
        public void MapEnumerationAndClearInvalidateAllReadKeys()
        {
            var map = Rx.Dictionary<string, int>(); map["a"] = 1; map["b"] = 2; int sum = 0, first = 0;
            this.owned.Observe(() => sum = map.Sum(pair => pair.Value));
            this.owned.Observe(() => { map.TryGetValue("a", out first); });
            map["a"] = 4; Assert.AreEqual(6, sum); map.Clear(); Assert.AreEqual(0, sum); Assert.AreEqual(0, first);
        }

        [Test]
        public void MapComparerAndDictionaryInterfaceAreConsistent()
        {
            var map = new ReactiveDictionary<string, int>(StringComparer.OrdinalIgnoreCase); int result = 0;
            this.owned.Observe(() => { map.TryGetValue("A", out result); }); map.Add("a", 1); Assert.AreEqual(1, result);
            IDictionary<string, int> api = map; Assert.Throws<ArgumentException>(() => api.Add("A", 2));
            Assert.IsFalse(api.Remove(new KeyValuePair<string, int>("A", 2)));
            Assert.IsTrue(api.Remove(new KeyValuePair<string, int>("A", 1))); Assert.AreEqual(0, result);
        }

        [Test]
        public void SetTracksMembershipIncludingNull()
        {
            var set = Rx.Set<string>(); int runs = 0; bool present = false;
            this.owned.Observe(() => { present = set.Contains(null); ++runs; });
            set.Add("x"); Assert.AreEqual(1, runs); set.Add(null); Assert.IsTrue(present);
            Assert.IsFalse(set.Add(null)); Assert.AreEqual(2, runs); set.Clear(); Assert.IsFalse(present);
        }

        [Test]
        public void SetTracksNullableValueTypeNullForBothCachePolicies()
        {
            foreach (var policy in new[] { KeySignalCachePolicy.Weak, KeySignalCachePolicy.Strong })
            {
                var set = Rx.Set<int?>(policy); int runs = 0; bool present = false;
                this.owned.Observe(() => { present = set.Contains(null); ++runs; });

                set.Add(1); Assert.AreEqual(1, runs);
                set.Add(null); Assert.IsTrue(present); Assert.AreEqual(2, runs);
                set.Remove(null); Assert.IsFalse(present); Assert.AreEqual(3, runs);
            }
        }

        [Test]
        public void SetBulkOperationsCoalesceAndHandleSelfOperands()
        {
            var set = Rx.Set<int>(); int runs = 0;
            this.owned.Observe(() => { _ = set.Count; ++runs; });
            set.UnionWith(new[] { 1, 2, 3 }); Assert.AreEqual(2, runs);
            set.IntersectWith(new[] { 2, 3, 4 }); Assert.IsTrue(set.SetEquals(new[] { 2, 3 }));
            set.SymmetricExceptWith(new[] { 3, 4 }); Assert.IsTrue(set.SetEquals(new[] { 2, 4 }));
            set.ExceptWith(set); Assert.IsEmpty(set); Assert.AreEqual(5, runs);
        }

        [Test]
        public void SetRelationsTrackChangesAndRespectComparer()
        {
            var set = new ReactiveSet<string>(StringComparer.OrdinalIgnoreCase); bool contains = false;
            this.owned.Observe(() => contains = set.IsSupersetOf(new[] { "a" }));
            set.Add("A"); Assert.IsTrue(contains); Assert.IsFalse(set.Add("a"));
            Assert.IsTrue(set.IsSubsetOf(new[] { "a", "b" })); Assert.IsTrue(set.IsProperSubsetOf(new[] { "a", "b" }));
            Assert.IsTrue(set.IsProperSupersetOf(Array.Empty<string>())); Assert.IsTrue(set.Overlaps(new[] { "a" }));
            set.Remove("a"); Assert.IsFalse(contains);
        }

        [Test]
        public void ExplicitCollectionSourceIncludesValueReplacement()
        {
            var list = new ReactiveCollection<int>(new[] { 1 }); int runs = 0;
            this.owned.Observe(() => ++runs, list); list[0] = 2; Assert.AreEqual(2, runs);
        }

        private sealed class ParityComparer : IEqualityComparer<int>
        {
            public bool Equals(int x, int y)
            {
                return (x & 1) == (y & 1);
            }

            public int GetHashCode(int value)
            {
                return value & 1;
            }
        }
    }
}
