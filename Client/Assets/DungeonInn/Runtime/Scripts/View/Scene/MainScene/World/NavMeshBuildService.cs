using System;
using System.Collections.Generic;
using DungeonInn.Domain.Map;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class NavMeshBuildService
    {
        readonly MapLayerViewRegistry layerViewRegistry;
        readonly HashSet<int> bakedLayerIds = new();

        [Inject]
        public NavMeshBuildService(MapLayerViewRegistry layerViewRegistry)
        {
            this.layerViewRegistry = layerViewRegistry ?? throw new ArgumentNullException(nameof(layerViewRegistry));
        }

        public void BakeLayerIfNeeded(MapLayerId layerId)
        {
            if (bakedLayerIds.Contains(layerId.Value))
            {
                return;
            }

            var tileRoot = layerViewRegistry.GetTileRoot(layerId);
            if (tileRoot == null)
            {
                return;
            }

            var surface = tileRoot.GetComponent<NavMeshSurface>();
            if (surface == null)
            {
                surface = tileRoot.gameObject.AddComponent<NavMeshSurface>();
            }

            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            surface.BuildNavMesh();

            bakedLayerIds.Add(layerId.Value);
            Debug.Log($"[DungeonInn] NavMesh baked for layer {layerId.Value}");
        }

        public void InvalidateLayer(MapLayerId layerId)
        {
            bakedLayerIds.Remove(layerId.Value);
        }
    }
}
