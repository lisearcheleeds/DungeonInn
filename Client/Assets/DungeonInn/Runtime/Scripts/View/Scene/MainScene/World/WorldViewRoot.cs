using System;
using DungeonInn.Domain.Map;
using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldViewRoot : IDisposable
    {
        readonly GameObject root;
        readonly GameObject layersRoot;

        public WorldViewRoot()
        {
            root = new GameObject("WorldViewRoot");
            root.layer = WorldRenderingLayer.Layer;
            layersRoot = new GameObject("Layers");
            layersRoot.layer = WorldRenderingLayer.Layer;
            layersRoot.transform.SetParent(root.transform, false);
        }

        public Transform LayersRoot => layersRoot.transform;

        public MapLayerViewRoot CreateLayerRoot(MapLayerId layerId, string layerName, Vector3 position)
        {
            var layerRoot = new GameObject($"{layerName}_{layerId.Value}");
            layerRoot.layer = WorldRenderingLayer.Layer;
            layerRoot.transform.position = position;
            layerRoot.transform.SetParent(LayersRoot, true);

            var tileRoot = new GameObject("Tiles");
            tileRoot.layer = WorldRenderingLayer.Layer;
            tileRoot.transform.SetParent(layerRoot.transform, false);

            var actorRoot = new GameObject("Actors");
            actorRoot.layer = WorldRenderingLayer.Layer;
            actorRoot.transform.SetParent(layerRoot.transform, false);

            return new MapLayerViewRoot(layerRoot.transform, tileRoot.transform, actorRoot.transform);
        }

        public void Dispose()
        {
            if (root != null)
            {
                UnityEngine.Object.Destroy(root);
            }
        }
    }
}
