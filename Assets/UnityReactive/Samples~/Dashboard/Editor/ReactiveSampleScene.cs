// <copyright file="ReactiveSampleScene.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace AillieoUtils.Reactive.Samples
{
    using System;
    using UnityEditor;
    using UnityEngine;

    public static class ReactiveSampleScene
    {
        [MenuItem("Tools/Reactive/Add Dashboard Sample")]
        public static void Open()
        {
            var prefab = FindDashboardPrefab();
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    "Reactive Dashboard prefab was not found. Import the Reactive Dashboard sample from Package Manager first.");
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Add Reactive Dashboard");
            Selection.activeObject = instance;
        }

        private static GameObject FindDashboardPrefab()
        {
            foreach (var guid in AssetDatabase.FindAssets("ReactiveDashboard t:Prefab"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && prefab.GetComponent<ReactiveDashboardSample>() != null)
                {
                    return prefab;
                }
            }

            return null;
        }
    }
}
