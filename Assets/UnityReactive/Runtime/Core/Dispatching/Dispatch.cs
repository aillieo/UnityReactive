// <copyright file="Dispatch.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive
{
    using System;
    using System.Collections.Generic;

    internal static class Dispatch
    {
        internal static int Depth;
        private static bool flushing;
        private static DispatchNode highFirst;
        private static DispatchNode highLast;
        private static DispatchNode normalFirst;
        private static DispatchNode normalLast;
        private static List<Exception> flushErrors;

        internal static bool HasPriority => highFirst != null;

        internal static bool IsIdle => Depth == 0 && !flushing && highFirst == null && normalFirst == null;

        internal static void Enqueue(DispatchNode node, bool priority)
        {
#if UNITY_5_3_OR_NEWER
            bool profiling = ReactiveProfiler.Enabled;
            if (profiling)
            {
                ReactiveProfiler.DispatchEnqueue.Begin();
            }
#endif
            if (!node.Queued)
            {
                node.Queued = true;
                if (priority)
                {
                    if (highLast == null)
                    {
                        highFirst = node;
                    }
                    else
                    {
                        highLast.QueueNext = node;
                    }

                    highLast = node;
                }
                else
                {
                    if (normalLast == null)
                    {
                        normalFirst = node;
                    }
                    else
                    {
                        normalLast.QueueNext = node;
                    }

                    normalLast = node;
                }
            }
#if UNITY_5_3_OR_NEWER
            if (profiling)
            {
                ReactiveProfiler.DispatchEnqueue.End();
            }
#endif
        }

        internal static void Flush()
        {
            if (Depth != 0 || flushing || (highFirst == null && normalFirst == null))
            {
                return;
            }

#if UNITY_5_3_OR_NEWER
            bool profiling = ReactiveProfiler.Enabled;
            if (profiling)
            {
                ReactiveProfiler.DispatchFlush.Begin();
            }
#endif
            flushing = true;
            flushErrors = null;
            List<Exception> errors;
            try
            {
                int count = 0;
                while (highFirst != null || normalFirst != null)
                {
                    if (++count > 10000)
                    {
                        throw new InvalidOperationException("Reactive update cycle exceeded 10000 callbacks.");
                    }

                    var node = Dequeue();
                    try
                    {
                        InvokeUntracked(node);
                    }
                    catch (Exception e)
                    {
                        Report(e);
                    }
                }
            }
            finally
            {
                errors = flushErrors;
                flushErrors = null;
                Clear(ref highFirst, ref highLast);
                Clear(ref normalFirst, ref normalLast);
                flushing = false;
#if UNITY_5_3_OR_NEWER
                if (profiling)
                {
                    ReactiveProfiler.DispatchFlush.End();
                }
#endif
            }

            if (errors != null)
            {
                throw new AggregateException("Reactive notifications failed.", errors);
            }
        }

        internal static void Report(Exception error)
        {
            (flushErrors ?? (flushErrors = new List<Exception>())).Add(error);
        }

        private static DispatchNode Dequeue()
        {
            DispatchNode node;
            if (highFirst != null)
            {
                node = highFirst;
                highFirst = node.QueueNext;
                if (highFirst == null)
                {
                    highLast = null;
                }
            }
            else
            {
                node = normalFirst;
                normalFirst = node.QueueNext;
                if (normalFirst == null)
                {
                    normalLast = null;
                }
            }

            node.QueueNext = null;
            node.Queued = false;
            return node;
        }

        private static void Clear(ref DispatchNode first, ref DispatchNode last)
        {
            while (first != null)
            {
                var next = first.QueueNext;
                first.QueueNext = null;
                first.Queued = false;
                first = next;
            }

            last = null;
        }

        private static void InvokeUntracked(DispatchNode node)
        {
            var previous = Tracking.Current;
            if (previous == null)
            {
                node.Invoke();
                return;
            }

            Tracking.Current = null;
            try
            {
                node.Invoke();
            }
            finally
            {
                Tracking.Current = previous;
            }
        }
    }
}
