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
        readonly HashSet<ActorBehaviorType> warnedPlaceholderFallbacks = new();
        readonly HashSet<ActorBehaviorType> warnedSpriteSetFallbacks = new();
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
                var spriteSetSprite = spriteSet.GetSprite(direction, isWalking, walkFrameIndex);
                if (spriteSetSprite == spriteSet.FallbackSprite && warnedSpriteSetFallbacks.Add(behaviorType))
                {
                    Debug.LogWarning(
                        $"[ActorSpriteVisualConfig] Using fallback actor sprite from sprite set. BehaviorType={behaviorType}");
                }

                return spriteSetSprite;
            }

            WarnPlaceholderFallback(behaviorType);
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

            WarnPlaceholderFallback(behaviorType);
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
                    DisposeUnityObject(sprite);
                }
            }

            foreach (var texture in placeholderTextures)
            {
                if (texture != null)
                {
                    DisposeUnityObject(texture);
                }
            }

            placeholderSprites.Clear();
            placeholderTextures.Clear();
            warnedPlaceholderFallbacks.Clear();
            warnedSpriteSetFallbacks.Clear();
        }

        void Add(ActorBehaviorType behaviorType, Color color, ActorVisualSizeTier visualSizeTier)
        {
            var sprite = ActorSpritePlaceholderFactory.Create(color, out var texture);
            placeholderTextures.Add(texture);
            placeholderSprites.Add(behaviorType, sprite);
            placeholderSizeTiers.Add(behaviorType, visualSizeTier);
        }

        void WarnPlaceholderFallback(ActorBehaviorType behaviorType)
        {
            if (!warnedPlaceholderFallbacks.Add(behaviorType))
            {
                return;
            }

            Debug.LogWarning($"[ActorSpriteVisualConfig] Using placeholder actor sprite. BehaviorType={behaviorType}");
        }

        static void DisposeUnityObject(UnityEngine.Object target)
        {
#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(target);
                return;
            }
#endif
            UnityEngine.Object.Destroy(target);
        }
    }
}
