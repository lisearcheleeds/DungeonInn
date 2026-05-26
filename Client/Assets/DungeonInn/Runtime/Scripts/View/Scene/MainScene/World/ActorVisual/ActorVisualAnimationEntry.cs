using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    [Serializable]
    public sealed class ActorVisualAnimationEntry
    {
        [SerializeField] ActorAnimationDirection direction;
        [SerializeField] float fps = 4f;
        [SerializeField] bool loop = true;
        [SerializeField] Sprite[] sprites = Array.Empty<Sprite>();

        public ActorAnimationDirection Direction => direction;
        public float Fps => fps;
        public bool Loop => loop;
        public IReadOnlyList<Sprite> Sprites => sprites ?? Array.Empty<Sprite>();
    }
}
