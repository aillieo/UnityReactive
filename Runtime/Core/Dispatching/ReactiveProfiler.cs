// <copyright file="ReactiveProfiler.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive
{
#if UNITY_5_3_OR_NEWER
    internal static class ReactiveProfiler
    {
        public const bool GlobalEnabled = true;

        internal static readonly Unity.Profiling.ProfilerMarker SignalNotify = new Unity.Profiling.ProfilerMarker("Reactive.Signal.Notify");
        internal static readonly Unity.Profiling.ProfilerMarker SignalDispatchNormal = new Unity.Profiling.ProfilerMarker("Reactive.Signal.DispatchNormal");
        internal static readonly Unity.Profiling.ProfilerMarker DispatchEnqueue = new Unity.Profiling.ProfilerMarker("Reactive.Dispatch.Enqueue");
        internal static readonly Unity.Profiling.ProfilerMarker DispatchFlush = new Unity.Profiling.ProfilerMarker("Reactive.Dispatch.Flush");
        internal static readonly Unity.Profiling.ProfilerMarker TrackingCapture = new Unity.Profiling.ProfilerMarker("Reactive.Tracking.Capture");
        internal static readonly Unity.Profiling.ProfilerMarker PropertySubscriptionRun = new Unity.Profiling.ProfilerMarker("Reactive.PropertySubscription.Run");
        internal static readonly Unity.Profiling.ProfilerMarker DependencySynchronize = new Unity.Profiling.ProfilerMarker("Reactive.DependencyTracker.Synchronize");
        internal static readonly Unity.Profiling.ProfilerMarker ComputedEvaluate = new Unity.Profiling.ProfilerMarker("Reactive.Computed.Evaluate");

        private static bool enabled = true;

        internal static bool Enabled
        {
            get => enabled && GlobalEnabled;
            set => enabled = value;
        }
    }

#endif
}
