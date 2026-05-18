using System;
using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class ActorSpriteSet
    {
        const int DirectionCount = 4;
        const int WalkFrameCount = 2;

        readonly Sprite[] idleSprites = new Sprite[DirectionCount];
        readonly Sprite[,] walkSprites = new Sprite[DirectionCount, WalkFrameCount];

        public ActorSpriteSet(Sprite fallbackSprite, ActorVisualSizeTier visualSizeTier)
        {
            FallbackSprite = fallbackSprite ?? throw new ArgumentNullException(nameof(fallbackSprite));
            VisualSizeTier = visualSizeTier;
        }

        public Sprite FallbackSprite { get; }
        public ActorVisualSizeTier VisualSizeTier { get; }

        public void SetIdleSprite(ActorAnimationDirection direction, Sprite sprite)
        {
            idleSprites[(int)direction] = sprite;
        }

        public void SetWalkSprite(ActorAnimationDirection direction, int frameIndex, Sprite sprite)
        {
            if (frameIndex < 0 || WalkFrameCount <= frameIndex)
            {
                throw new ArgumentOutOfRangeException(nameof(frameIndex));
            }

            walkSprites[(int)direction, frameIndex] = sprite;
        }

        public Sprite GetSprite(
            ActorAnimationDirection direction,
            bool isWalking,
            int walkFrameIndex)
        {
            var directionIndex = (int)direction;
            if (isWalking)
            {
                var frameIndex = Mathf.Abs(walkFrameIndex) % WalkFrameCount;
                return walkSprites[directionIndex, frameIndex] ??
                    idleSprites[directionIndex] ??
                    FallbackSprite;
            }

            return idleSprites[directionIndex] ?? FallbackSprite;
        }
    }
}
