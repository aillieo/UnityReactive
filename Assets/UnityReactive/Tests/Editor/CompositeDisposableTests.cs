// <copyright file="CompositeDisposableTests.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace AillieoUtils.Reactive.Tests
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;

    public sealed class CompositeDisposableTests
    {
        [Test]
        public void ClearDisposesInReverseOrderAndRemainsReusable()
        {
            var order = new List<int>();
            var owner = new CompositeDisposable();
            owner.Add(new CallbackDisposable(() => order.Add(1)));
            owner.Add(new CallbackDisposable(() => order.Add(2)));

            owner.Clear();

            CollectionAssert.AreEqual(new[] { 2, 1 }, order);
            Assert.AreEqual(0, owner.Count);
            Assert.IsFalse(owner.IsDisposed);

            owner.Add(new CallbackDisposable(() => order.Add(3)));
            owner.Clear();
            CollectionAssert.AreEqual(new[] { 2, 1, 3 }, order);
        }

        [Test]
        public void DisposeClosesOwnerAndLateAddsAreDisposedImmediately()
        {
            var owner = new CompositeDisposable();
            int disposed = 0;
            owner.Add(new CallbackDisposable(() => ++disposed));

            owner.Dispose();
            owner.Dispose();
            var late = owner.Add(new CallbackDisposable(() => ++disposed));

            Assert.IsTrue(owner.IsDisposed);
            Assert.AreEqual(0, owner.Count);
            Assert.AreEqual(2, disposed);
            Assert.IsTrue(late.IsDisposed);
        }

        [Test]
        public void RemoveDisposesOnlyTheRemovedResource()
        {
            var owner = new CompositeDisposable();
            var first = owner.Add(new CallbackDisposable(null));
            var second = owner.Add(new CallbackDisposable(null));

            Assert.IsTrue(owner.Remove(first));
            Assert.IsTrue(first.IsDisposed);
            Assert.IsFalse(second.IsDisposed);
            Assert.AreEqual(1, owner.Count);
            Assert.IsFalse(owner.Remove(first));

            owner.Dispose();
            Assert.IsTrue(second.IsDisposed);
        }

        [Test]
        public void DisposalContinuesAfterErrorsAndOwnerStillCloses()
        {
            var owner = new CompositeDisposable();
            bool finalResourceDisposed = false;
            owner.Add(new CallbackDisposable(() => finalResourceDisposed = true));
            owner.Add(new CallbackDisposable(() => throw new InvalidOperationException("expected")));

            var error = Assert.Throws<AggregateException>(() => owner.Dispose());

            Assert.AreEqual(1, error.InnerExceptions.Count);
            Assert.IsTrue(finalResourceDisposed);
            Assert.IsTrue(owner.IsDisposed);
            Assert.AreEqual(0, owner.Count);
        }

        [Test]
        public void ResourceAddedDuringClearBelongsToNextLifetime()
        {
            var owner = new CompositeDisposable();
            CallbackDisposable added = null;
            owner.Add(new CallbackDisposable(() => added = owner.Add(new CallbackDisposable(null))));

            owner.Clear();

            Assert.IsNotNull(added);
            Assert.IsFalse(added.IsDisposed);
            Assert.AreEqual(1, owner.Count);
            owner.Clear();
            Assert.IsTrue(added.IsDisposed);
        }

        [Test]
        public void ResourceAddedDuringDisposeIsReleasedImmediately()
        {
            var owner = new CompositeDisposable();
            CallbackDisposable added = null;
            owner.Add(new CallbackDisposable(() => added = owner.Add(new CallbackDisposable(null))));

            owner.Dispose();

            Assert.IsNotNull(added);
            Assert.IsTrue(added.IsDisposed);
            Assert.AreEqual(0, owner.Count);
        }

        [Test]
        public void AddToSupportsPlainOwnershipWithoutReactiveScope()
        {
            var owner = new CompositeDisposable();
            var resource = new CallbackDisposable(null).AddTo(owner);

            Assert.AreEqual(1, owner.Count);
            owner.Dispose();
            Assert.IsTrue(resource.IsDisposed);
        }

        [Test]
        public void DisposingSeparateOwnersDoesNotRedisposeEarlierResources()
        {
            var disposed = new List<int>();
            for (int id = 1; id <= 3; ++id)
            {
                var owner = new CompositeDisposable();
                int capturedId = id;
                owner.Add(new NonIdempotentDisposable(() => disposed.Add(capturedId)));
                owner.Dispose();
            }

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, disposed);
        }

        [Test]
        public void ScopeAddAfterDisposeReleasesResourceButObserveDoesNotRun()
        {
            var scope = new ReactiveScope(null);
            scope.Dispose();
            var resource = scope.Add(new CallbackDisposable(null));
            int runs = 0;

            Assert.IsTrue(resource.IsDisposed);
            Assert.Throws<ObjectDisposedException>(() => scope.Observe(() => ++runs));
            Assert.AreEqual(0, runs);
        }

        private sealed class CallbackDisposable : IDisposable
        {
            private readonly Action callback;

            internal CallbackDisposable(Action callback)
            {
                this.callback = callback;
            }

            internal bool IsDisposed { get; private set; }

            public void Dispose()
            {
                if (this.IsDisposed)
                {
                    return;
                }

                this.IsDisposed = true;
                this.callback?.Invoke();
            }
        }

        private sealed class NonIdempotentDisposable : IDisposable
        {
            private readonly Action callback;

            internal NonIdempotentDisposable(Action callback)
            {
                this.callback = callback;
            }

            public void Dispose()
            {
                this.callback();
            }
        }
    }
}
