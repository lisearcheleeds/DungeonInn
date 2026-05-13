using System;
using System.Collections.Generic;
using DungeonInn.Domain.Actor;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class ActorSpriteVisualConfig : IDisposable
    {
        const int SpriteWidth = 32;
        const int SpriteHeight = 48;
        const float PixelsPerUnit = 16f;

        readonly Dictionary<ActorBehaviorType, Sprite> placeholderSprites = new();
        readonly List<Texture2D> placeholderTextures = new();

        [Inject]
        public ActorSpriteVisualConfig()
        {
            Add(ActorBehaviorType.Adventurer, new Color(0.1f, 0.45f, 1f, 1f));
            Add(ActorBehaviorType.Monster, new Color(0.9f, 0.15f, 0.1f, 1f));
            Add(ActorBehaviorType.None, new Color(1f, 0.85f, 0.1f, 1f));
        }

        public Sprite GetPlaceholderSprite(Actor actor)
        {
            var behaviorType = ResolveBehaviorType(actor);
            if (!placeholderSprites.TryGetValue(behaviorType, out var sprite))
            {
                return placeholderSprites[ActorBehaviorType.None];
            }

            return sprite;
        }

        public void Dispose()
        {
            foreach (var sprite in placeholderSprites.Values)
            {
                if (sprite != null)
                {
                    UnityEngine.Object.Destroy(sprite);
                }
            }

            foreach (var texture in placeholderTextures)
            {
                if (texture != null)
                {
                    UnityEngine.Object.Destroy(texture);
                }
            }

            placeholderSprites.Clear();
            placeholderTextures.Clear();
        }

        static ActorBehaviorType ResolveBehaviorType(Actor actor)
        {
            if (actor.Behavior is AdventurerBehavior)
            {
                return ActorBehaviorType.Adventurer;
            }

            if (actor.Behavior is MonsterBehavior)
            {
                return ActorBehaviorType.Monster;
            }

            return ActorBehaviorType.None;
        }

        void Add(ActorBehaviorType behaviorType, Color color)
        {
            var texture = CreatePlaceholderTexture(color);
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, SpriteWidth, SpriteHeight),
                new Vector2(0.5f, 0f),
                PixelsPerUnit);
            placeholderTextures.Add(texture);
            placeholderSprites.Add(behaviorType, sprite);
        }

        static Texture2D CreatePlaceholderTexture(Color color)
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
