using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    public static class PlaceholderAssetFactory
    {
        const int ActorSpriteWidth = 32;
        const int ActorSpriteHeight = 48;
        const int ProjectileSpriteSize = 8;
        const int AreaEffectSpriteSize = 16;
        const float PixelsPerUnit = 16f;

        public static Sprite CreateActorPlaceholder(Color color, out Texture2D texture)
        {
            texture = CreateTexture(color, ActorSpriteWidth, ActorSpriteHeight);
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, ActorSpriteWidth, ActorSpriteHeight),
                new Vector2(0.5f, 0f),
                PixelsPerUnit);
        }

        public static Sprite CreateProjectilePlaceholder(Color color, out Texture2D texture)
        {
            texture = CreateTexture(color, ProjectileSpriteSize, ProjectileSpriteSize);
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, ProjectileSpriteSize, ProjectileSpriteSize),
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit);
        }

        public static Sprite CreateAreaEffectPlaceholder(Color color, out Texture2D texture)
        {
            texture = CreateTexture(color, AreaEffectSpriteSize, AreaEffectSpriteSize);
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, AreaEffectSpriteSize, AreaEffectSpriteSize),
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit);
        }

        static Texture2D CreateTexture(Color color, int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[width * height];
            for (var index = 0; index < pixels.Length; index++)
            {
                pixels[index] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
