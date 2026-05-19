using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    public static class ActorSpritePlaceholderFactory
    {
        const int SpriteWidth = 32;
        const int SpriteHeight = 48;
        const float PixelsPerUnit = 16f;

        public static Sprite Create(Color color, out Texture2D texture)
        {
            texture = CreateTexture(color);
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, SpriteWidth, SpriteHeight),
                new Vector2(0.5f, 0f),
                PixelsPerUnit);
        }

        static Texture2D CreateTexture(Color color)
        {
            var texture = new Texture2D(SpriteWidth, SpriteHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[SpriteWidth * SpriteHeight];
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
