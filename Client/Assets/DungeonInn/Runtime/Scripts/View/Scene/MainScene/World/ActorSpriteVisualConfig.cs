using System;
using System.Collections.Generic;
using DungeonInn.Domain.Actor;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class ActorSpriteVisualConfig : IDisposable
    {
        readonly Dictionary<ActorBehaviorType, Sprite> placeholderSprites = new();
        readonly Dictionary<ActorBehaviorType, ActorVisualSizeTier> placeholderSizeTiers = new();
        readonly List<Texture2D> placeholderTextures = new();
        readonly VisualConfigLoader visualConfigLoader;

        [Inject]
        public ActorSpriteVisualConfig(VisualConfigLoader visualConfigLoader)
        {
            this.visualConfigLoader = visualConfigLoader ?? throw new ArgumentNullException(nameof(visualConfigLoader));

            Add(ActorBehaviorType.Adventurer, new Color(0.1f, 0.45f, 1f, 1f), ActorVisualSizeTier.AdventurerS);
            Add(ActorBehaviorType.Monster, new Color(0.9f, 0.15f, 0.1f, 1f), ActorVisualSizeTier.MonsterS);
            Add(ActorBehaviorType.GuildStaff, new Color(0.1f, 0.8f, 0.4f, 1f), ActorVisualSizeTier.AdventurerS);
            Add(ActorBehaviorType.Pet, new Color(0.8f, 0.5f, 0.1f, 1f), ActorVisualSizeTier.MonsterS);
            Add(ActorBehaviorType.None, new Color(1f, 0.85f, 0.1f, 1f), ActorVisualSizeTier.AdventurerS);
        }

        public Sprite GetSprite(
            ActorBehaviorType behaviorType,
            ActorAnimationDirection direction,
            bool isWalking,
            int walkFrameIndex)
        {
            var spriteSet = visualConfigLoader.GetSpriteSet(behaviorType);
            if (spriteSet != null)
            {
                return spriteSet.GetSprite(direction, isWalking, walkFrameIndex);
            }

            if (!placeholderSprites.TryGetValue(behaviorType, out var sprite))
            {
                return placeholderSprites[ActorBehaviorType.None];
            }

            return sprite;
        }

        public ActorVisualSizeTier GetVisualSizeTier(ActorBehaviorType behaviorType)
        {
            var spriteSet = visualConfigLoader.GetSpriteSet(behaviorType);
            if (spriteSet != null)
            {
                return spriteSet.VisualSizeTier;
            }

            if (!placeholderSizeTiers.TryGetValue(behaviorType, out var sizeTier))
            {
                return placeholderSizeTiers[ActorBehaviorType.None];
            }

            return sizeTier;
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

        void Add(ActorBehaviorType behaviorType, Color color, ActorVisualSizeTier visualSizeTier)
        {
            var sprite = ActorSpritePlaceholderFactory.Create(color, out var texture);
            placeholderTextures.Add(texture);
            placeholderSprites.Add(behaviorType, sprite);
            placeholderSizeTiers.Add(behaviorType, visualSizeTier);
        }
    }
}
