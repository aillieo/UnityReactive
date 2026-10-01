// <copyright file="Tracking.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive
{
    using System;
    using System.Collections.Generic;

    internal static class Tracking
    {
        internal static DependencyCollector Current;
        private static readonly Queue<Action> posts = new Queue<Action>();
        private static int depth;
        private static bool posting;
        private static long contextSequence;
        private static long currentContext;
        private static long pauseSequence;
        private static long currentPause;

        internal static bool HasPendingPosts => posts.Count != 0;

        internal static T Capture<T>(Func<T> action, DependencyCollector dependencies)
        {
#if UNITY_5_3_OR_NEWER
            bool profiling = ReactiveProfiler.Enabled;
            if (profiling)
            {
                ReactiveProfiler.TrackingCapture.Begin();
            }
#endif
            var previous = Current;
            long previousContext = currentContext;
            currentContext = unchecked(++contextSequence);
            bool trackingChanged = !ReferenceEquals(previous, dependencies);
            if (trackingChanged)
            {
                Current = dependencies;
            }

            ++depth;
            try
            {
                return action();
            }
            catch
            {
                if (depth == 1)
                {
                    posts.Clear();
                }

                throw;
            }
            finally
            {
                --depth;
                if (trackingChanged)
                {
                    Current = previous;
                }

                currentContext = previousContext;
#if UNITY_5_3_OR_NEWER
                if (profiling)
                {
                    ReactiveProfiler.TrackingCapture.End();
                }
#endif
            }
        }

        internal static void Post(Action action)
        {
            if (action != null)
            {
                posts.Enqueue(action);
            }

            if (depth != 0 || posting || posts.Count == 0)
            {
                return;
            }

            posting = true;
            List<Exception> errors = null;
            try
            {
                int count = 0;
                while (posts.Count != 0)
                {
                    if (++count > 10000)
                    {
                        throw new InvalidOperationException("Reactive post-effect cycle detected.");
                    }

                    try
                    {
                        Untracked(posts.Dequeue());
                    }
                    catch (Exception e)
                    {
                        (errors ?? (errors = new List<Exception>())).Add(e);
                    }
                }
            }
            finally
            {
                posts.Clear();
                posting = false;
            }

            if (errors != null)
            {
                throw new AggregateException(errors);
            }
        }

        internal static void Disconnect(List<IDisposable> handles)
        {
            foreach (var handle in handles)
            {
                handle.Dispose();
            }

            handles.Clear();
        }

        internal static void Capture(Action action, DependencyCollector dependencies)
        {
#if UNITY_5_3_OR_NEWER
            bool profiling = ReactiveProfiler.Enabled;
            if (profiling)
            {
                ReactiveProfiler.TrackingCapture.Begin();
            }
#endif
            var previous = Current;
            long previousContext = currentContext;
            currentContext = unchecked(++contextSequence);
            bool trackingChanged = !ReferenceEquals(previous, dependencies);
            if (trackingChanged)
            {
                Current = dependencies;
            }

            ++depth;
            try
            {
                action();
            }
            catch
            {
                if (depth == 1)
                {
                    posts.Clear();
                }

                throw;
            }
            finally
            {
                --depth;
                if (trackingChanged)
                {
                    Current = previous;
                }

                currentContext = previousContext;
#if UNITY_5_3_OR_NEWER
                if (profiling)
                {
                    ReactiveProfiler.TrackingCapture.End();
                }
#endif
            }
        }

        internal static void CaptureProperty<T>(IReadOnlyReactiveProperty<T> source, Action<T> callback)
        {
#if UNITY_5_3_OR_NEWER
            bool profiling = ReactiveProfiler.Enabled;
            if (profiling)
            {
                ReactiveProfiler.TrackingCapture.Begin();
            }
#endif
            var previous = Current;
            long previousContext = currentContext;
            currentContext = unchecked(++contextSequence);
            if (previous != null)
            {
                Current = null;
            }

            ++depth;
            try
            {
                callback(source.Value);
            }
            catch
            {
                if (depth == 1)
                {
                    posts.Clear();
                }

                throw;
            }
            finally
            {
                --depth;
                if (previous != null)
                {
                    Current = previous;
                }

                currentContext = previousContext;
#if UNITY_5_3_OR_NEWER
                if (profiling)
                {
                    ReactiveProfiler.TrackingCapture.End();
                }
#endif
            }
        }

        internal static void CapturePropertyValue<T>(T value, Action<T> callback)
        {
#if UNITY_5_3_OR_NEWER
            bool profiling = ReactiveProfiler.Enabled;
            if (profiling)
            {
                ReactiveProfiler.TrackingCapture.Begin();
            }
#endif
            var previous = Current;
            long previousContext = currentContext;
            currentContext = unchecked(++contextSequence);
            if (previous != null)
            {
                Current = null;
            }

            ++depth;
            try
            {
                callback(value);
            }
            catch
            {
                if (depth == 1)
                {
                    posts.Clear();
                }

                throw;
            }
            finally
            {
                --depth;
                if (previous != null)
                {
                    Current = previous;
                }

                currentContext = previousContext;
#if UNITY_5_3_OR_NEWER
                if (profiling)
                {
                    ReactiveProfiler.TrackingCapture.End();
                }
#endif
            }
        }

        internal static T CapturePropertyChange<T>(
            IReadOnlyReactiveProperty<T> source,
            bool hasPreviousValue,
            T oldValue,
            Action<PropertyChange<T>> callback)
        {
            var previous = Current;
            long previousContext = currentContext;
            currentContext = unchecked(++contextSequence);
            if (previous != null)
            {
                Current = null;
            }

            ++depth;
            try
            {
                T current = source.Value;
                callback(new PropertyChange<T>(hasPreviousValue, oldValue, current));
                return current;
            }
            catch
            {
                if (depth == 1)
                {
                    posts.Clear();
                }

                throw;
            }
            finally
            {
                --depth;
                if (previous != null)
                {
                    Current = previous;
                }

                currentContext = previousContext;
            }
        }

        internal static IDisposable Pause()
        {
            return new PauseHandle();
        }

        internal static void Untracked(Action action)
        {
            if (Current == null)
            {
                action();
                return;
            }

            var previous = Current;
            Current = null;
            try
            {
                action();
            }
            finally
            {
                Current = previous;
            }
        }

        internal static T Untracked<T>(Func<T> action)
        {
            if (Current == null)
            {
                return action();
            }

            var previous = Current;
            Current = null;
            try
            {
                return action();
            }
            finally
            {
                Current = previous;
            }
        }

        private sealed class PauseHandle : IDisposable
        {
            private readonly DependencyCollector previous;
            private readonly long context;
            private readonly long previousPause;
            private readonly long pause;
            private bool disposed;

            internal PauseHandle()
            {
                this.previous = Current;
                this.context = currentContext;
                this.previousPause = currentPause;
                this.pause = unchecked(++pauseSequence);
                currentPause = this.pause;
                Current = null;
            }

            public void Dispose()
            {
                if (this.disposed)
                {
                    return;
                }

                if (currentPause != this.pause)
                {
                    throw new InvalidOperationException(
                        "Tracking pause scopes must be disposed in reverse creation order.");
                }

                this.disposed = true;
                currentPause = this.previousPause;
                if (currentContext != this.context || Current != null)
                {
                    throw new InvalidOperationException(
                        "A tracking pause scope cannot be disposed outside the context that created it.");
                }

                Current = this.previous;
            }
        }
    }
}
