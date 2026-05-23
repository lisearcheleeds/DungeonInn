using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    [CreateAssetMenu(menuName = "DungeonInn/Visual/MapMaterialSet")]
    public sealed class MapMaterialSetSO : ScriptableObject
    {
        [Header("Material Entries")]
        [SerializeField] List<Entry> entries = new();

        public IReadOnlyList<Entry> Entries => entries;

        [Serializable]
        public sealed class Entry
        {
            public TileVisualKind Kind;
            public string MaterialAddress;
            public Color FallbackColor;
        }
    }
}
