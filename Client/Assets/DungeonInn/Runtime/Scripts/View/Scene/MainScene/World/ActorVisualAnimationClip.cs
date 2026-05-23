using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class ActorVisualAnimationClip
    {
        readonly Sprite[] sprites;

        public ActorVisualAnimationClip(float fps, bool loop, IReadOnlyList<Sprite> sprites)
        {
            if (fps < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(fps));
            }

            Fps = fps;
            Loop = loop;
            if (sprites == null)
            {
                this.sprites = Array.Empty<Sprite>();
                return;
            }

            this.sprites = new Sprite[sprites.Count];
            for (var index = 0; index < sprites.Count; index++)
            {
                this.sprites[index] = sprites[index];
            }
        }

        public float Fps { get; }
        public bool Loop { get; }
        public int FrameCount => sprites.Length;

        public Sprite GetSprite(int frameIndex)
        {
            if (sprites.Length == 0)
            {
                return null;
            }

            if (frameIndex < 0)
            {
                return sprites[0];
            }

            if (frameIndex < sprites.Length)
            {
                return sprites[frameIndex];
            }

            return sprites[sprites.Length - 1];
        }
    }
}
