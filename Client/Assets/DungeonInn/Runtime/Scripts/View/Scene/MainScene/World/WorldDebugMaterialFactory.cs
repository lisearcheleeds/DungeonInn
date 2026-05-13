using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    public static class WorldDebugMaterialFactory
    {
        public static Material Create(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var material = new Material(shader);
            material.color = color;
            return material;
        }

        public static void Dispose(Material material)
        {
            if (material != null)
            {
                UnityEngine.Object.Destroy(material);
            }
        }
    }
}
