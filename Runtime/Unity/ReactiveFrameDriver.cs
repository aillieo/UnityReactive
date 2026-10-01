// <copyright file="ReactiveFrameDriver.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace AillieoUtils.Reactive.Unity
{
    using System;
    using UnityEngine;

    /// <summary>Flushes default watches once in LateUpdate, after ordinary gameplay Update calls.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(32000)]
    [AddComponentMenu("")]
    public sealed class ReactiveFrameDriver : MonoBehaviour
    {
        private static ReactiveFrameDriver instance;

        /// <summary>Ensures that the frame scheduler has exactly one persistent driver.</summary>
        public static void EnsureInstalled()
        {
            if (!Application.isPlaying || instance != null)
            {
                return;
            }

            instance = FindObjectOfType<ReactiveFrameDriver>();
            if (instance != null)
            {
                DontDestroyOnLoad(instance.gameObject);
                return;
            }

            var host = new GameObject("Reactive Frame Driver") { hideFlags = HideFlags.HideInHierarchy };
            instance = host.AddComponent<ReactiveFrameDriver>();
            DontDestroyOnLoad(host);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            Rx.FrameScheduler.Clear();
            Rx.FrameScheduler.ExceptionHandler = Debug.LogException;
            RxUnity.ResetStatics();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            EnsureInstalled();
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(this.gameObject);
                return;
            }

            if (instance != this)
            {
                Destroy(this);
            }
        }

        // LateUpdate keeps frame batching deterministic. PlayerLoop integration can be added if finer timing is needed.
        private void LateUpdate()
        {
            Rx.FrameScheduler.Flush();
        }

        private void Update()
        {
            try
            {
                RxUnity.AdvanceFrame(Time.deltaTime, Time.unscaledDeltaTime);
            }
            catch (AggregateException errors)
            {
                foreach (var error in errors.InnerExceptions)
                {
                    Debug.LogException(error);
                }
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void OnApplicationQuit()
        {
            Rx.FrameScheduler.Clear();
            RxUnity.ResetStatics();
        }
    }
}
