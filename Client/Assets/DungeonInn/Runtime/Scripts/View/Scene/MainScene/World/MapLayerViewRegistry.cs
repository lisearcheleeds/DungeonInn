using System;
using System.Collections.Generic;
using DungeonInn.Domain.Map;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class MapLayerViewRegistry : IDisposable
    {
        readonly WorldViewRoot viewRoot;
        readonly Dictionary<int, MapLayerViewRoot> layerRoots = new();
        readonly List<int> orderedLayerIds = new();

        int? activeLayerId;

        [Inject]
        public MapLayerViewRegistry(WorldViewRoot viewRoot)
        {
            this.viewRoot = viewRoot ?? throw new ArgumentNullException(nameof(viewRoot));
        }

        public Transform GetOrCreateTileRoot(MapLayerId layerId, string layerName)
        {
            return GetOrCreateLayerRoot(layerId, layerName).TileRoot;
        }

        public Transform GetTileRoot(MapLayerId layerId)
        {
            if (!layerRoots.TryGetValue(layerId.Value, out var root))
            {
                return null;
            }

            return root.TileRoot;
        }

        public Transform GetOrCreateActorRoot(MapLayerId layerId)
        {
            return GetOrCreateLayerRoot(layerId, ResolveFallbackLayerName(layerId)).ActorRoot;
        }

        public void DestroyLayerRoot(MapLayerId layerId)
        {
            if (!layerRoots.TryGetValue(layerId.Value, out var root))
            {
                return;
            }

            if (root.Root != null)
            {
                DestroyLayerObject(root.Root.gameObject);
            }

            layerRoots.Remove(layerId.Value);
            orderedLayerIds.Remove(layerId.Value);

            if (activeLayerId == layerId.Value)
            {
                activeLayerId = orderedLayerIds.Count > 0 ? orderedLayerIds[0] : (int?)null;
                ApplyLayerVisibility();
            }
        }

        public void SelectNextLayer()
        {
            SelectRelativeLayer(1);
        }

        public void SelectPreviousLayer()
        {
            SelectRelativeLayer(-1);
        }

        public void Dispose()
        {
            layerRoots.Clear();
            orderedLayerIds.Clear();
        }

        MapLayerViewRoot GetOrCreateLayerRoot(MapLayerId layerId, string layerName)
        {
            if (layerRoots.TryGetValue(layerId.Value, out var layerRoot))
            {
                return layerRoot;
            }

            layerRoot = viewRoot.CreateLayerRoot(
                layerId,
                layerName,
                Vector3.zero);
            layerRoots.Add(layerId.Value, layerRoot);
            AddOrderedLayerId(layerId.Value);
            activeLayerId ??= layerId.Value;
            ApplyLayerVisibility(layerRoot, layerId.Value);
            return layerRoot;
        }

        void SelectRelativeLayer(int direction)
        {
            if (orderedLayerIds.Count == 0)
            {
                return;
            }

            var currentIndex = ResolveActiveLayerIndex();
            var nextIndex = (currentIndex + direction + orderedLayerIds.Count) % orderedLayerIds.Count;
            activeLayerId = orderedLayerIds[nextIndex];
            ApplyLayerVisibility();
        }

        int ResolveActiveLayerIndex()
        {
            if (!activeLayerId.HasValue)
            {
                return 0;
            }

            var currentIndex = orderedLayerIds.IndexOf(activeLayerId.Value);
            return currentIndex < 0 ? 0 : currentIndex;
        }

        void AddOrderedLayerId(int layerId)
        {
            var insertIndex = 0;
            while (insertIndex < orderedLayerIds.Count && orderedLayerIds[insertIndex] < layerId)
            {
                insertIndex++;
            }

            orderedLayerIds.Insert(insertIndex, layerId);
        }

        void ApplyLayerVisibility()
        {
            foreach (var pair in layerRoots)
            {
                ApplyLayerVisibility(pair.Value, pair.Key);
            }
        }

        void ApplyLayerVisibility(MapLayerViewRoot layerRoot, int layerId)
        {
            layerRoot.Root.gameObject.SetActive(activeLayerId.HasValue && activeLayerId.Value == layerId);
        }

        static string ResolveFallbackLayerName(MapLayerId layerId)
        {
            if (layerId.Equals(MapLayerId.Ground))
            {
                return "Ground";
            }

            return $"Layer{layerId.Value}";
        }

        static void DestroyLayerObject(GameObject layerObject)
        {
#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(layerObject);
                return;
            }
#endif
            UnityEngine.Object.Destroy(layerObject);
        }
    }
}
