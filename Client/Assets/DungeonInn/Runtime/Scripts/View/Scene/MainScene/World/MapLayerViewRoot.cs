using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class MapLayerViewRoot
    {
        public Transform Root { get; }
        public Transform TileRoot { get; }
        public Transform ActorRoot { get; }

        public MapLayerViewRoot(Transform root, Transform tileRoot, Transform actorRoot)
        {
            Root = root;
            TileRoot = tileRoot;
            ActorRoot = actorRoot;
        }
    }
}
