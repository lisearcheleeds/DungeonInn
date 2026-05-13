using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    public static class WorldRenderingLayer
    {
        public const string LayerName = "World";

        public static int Layer
        {
            get
            {
                var layer = LayerMask.NameToLayer(LayerName);
                return layer < 0 ? 0 : layer;
            }
        }

        public static int Mask => 1 << Layer;
    }
}
