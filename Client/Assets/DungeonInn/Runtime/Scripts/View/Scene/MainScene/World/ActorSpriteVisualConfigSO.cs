using System;
using System.Collections.Generic;
using DungeonInn.Domain.Actor;
using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    [CreateAssetMenu(menuName = "DungeonInn/Visual/ActorSpriteVisualConfig")]
    public sealed class ActorSpriteVisualConfigSO : ScriptableObject
    {
        [SerializeField] ActorView actorPrefab;
        [SerializeField] List<Entry> entries = new();

        public ActorView ActorPrefab => actorPrefab;
        public IReadOnlyList<Entry> Entries => entries;

        [Serializable]
        public sealed class Entry
        {
            public ActorBehaviorType BehaviorType;
            public string AddressPrefix;
            public Color FallbackColor;
            public ActorVisualSizeTier VisualSizeTier;
        }
    }
}
