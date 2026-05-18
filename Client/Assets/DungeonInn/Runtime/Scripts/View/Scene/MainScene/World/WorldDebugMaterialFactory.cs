using System;
using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    public static class WorldDebugMaterialFactory
    {
        public static Material Create(Color color)
        {
            var shader = ResolveShader();

            var material = new Material(shader);
            material.color = color;
            return material;
        }

        public static void Dispose(Material material)
        {
            if (material == null)
            {
                return;
            }

            if (UnityEngine.Application.isPlaying)
            {
                UnityEngine.Object.Destroy(material);
                return;
            }

            UnityEngine.Object.DestroyImmediate(material);
        }

        static Shader ResolveShader()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
            {
                return shader;
            }

            shader = Shader.Find("Standard");
            if (shader != null)
            {
                return shader;
            }

            shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                return shader;
            }

            throw new InvalidOperationException("Debug material shader does not exist.");
        }
    }
}
