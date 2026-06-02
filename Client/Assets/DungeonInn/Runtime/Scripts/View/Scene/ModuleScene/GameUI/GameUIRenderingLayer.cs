using UnityEngine;

namespace DungeonInn.View.Scene.ModuleScene.GameUI
{
    internal static class GameUIRenderingLayer
    {
        const string LayerName = "UI";

        public static int Layer
        {
            get
            {
                var layer = LayerMask.NameToLayer(LayerName);
                return layer < 0 ? 0 : layer;
            }
        }

        public static void ApplyToHierarchy(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            var layer = Layer;
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = layer;
            }
        }
    }
}

