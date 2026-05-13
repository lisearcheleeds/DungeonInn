using System;
using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldViewRoot : IDisposable
    {
        readonly GameObject root;
        readonly GameObject tileRoot;
        readonly GameObject actorRoot;

        public WorldViewRoot()
        {
            root = new GameObject("WorldViewRoot");
            tileRoot = new GameObject("Tiles");
            actorRoot = new GameObject("Actors");
            tileRoot.transform.SetParent(root.transform, false);
            actorRoot.transform.SetParent(root.transform, false);
        }

        public Transform TileRoot => tileRoot.transform;
        public Transform ActorRoot => actorRoot.transform;

        public Transform CreateMapLayerRoot(string layerName, Vector3 position)
        {
            var layerRoot = new GameObject(layerName);
            layerRoot.transform.SetParent(TileRoot, false);
            layerRoot.transform.position = position;
            return layerRoot.transform;
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
