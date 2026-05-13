using System;
using System.Collections.Generic;
using DungeonInn.Domain.Map;
using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class MapLayerViewRegistry : IDisposable
    {
        readonly WorldViewRoot viewRoot;
        readonly LayerPositionViewMapper positionMapper;
        readonly Dictionary<int, MapLayerViewRoot> layerRoots = new();

        public MapLayerViewRegistry(WorldViewRoot viewRoot, LayerPositionViewMapper positionMapper)
        {
            this.viewRoot = viewRoot ?? throw new ArgumentNullException(nameof(viewRoot));
            this.positionMapper = positionMapper ?? throw new ArgumentNullException(nameof(positionMapper));
        }

        public Transform GetOrCreateTileRoot(MapLayerId layerId, string layerName)
        {
            return GetOrCreateLayerRoot(layerId, layerName).TileRoot;
        }

        public Transform GetOrCreateActorRoot(MapLayerId layerId)
        {
            return GetOrCreateLayerRoot(layerId, ResolveFallbackLayerName(layerId)).ActorRoot;
        }

        public void Dispose()
        {
            layerRoots.Clear();
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
                new Vector3(0f, positionMapper.ResolveLayerY(layerId), 0f));
            layerRoots.Add(layerId.Value, layerRoot);
            return layerRoot;
        }

        static string ResolveFallbackLayerName(MapLayerId layerId)
        {
            if (layerId.Equals(MapLayerId.Ground))
            {
                return "Ground";
            }

            return $"Layer{layerId.Value}";
        }
    }
}
