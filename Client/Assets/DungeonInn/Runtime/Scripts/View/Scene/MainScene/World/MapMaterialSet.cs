using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class MapMaterialSet : IDisposable
    {
        readonly Dictionary<TileVisualKind, Material> materials = new();

        public MapMaterialSet()
        {
            Add(TileVisualKind.GroundWalkable, new Color(0.24f, 0.32f, 0.24f, 0.45f));
            Add(TileVisualKind.GroundBlocked, new Color(0.25f, 0.25f, 0.25f, 0.65f));
            Add(TileVisualKind.DungeonWalkable, new Color(0.18f, 0.20f, 0.26f, 0.65f));
            Add(TileVisualKind.DungeonBlocked, new Color(0.10f, 0.10f, 0.12f, 0.75f));
            Add(TileVisualKind.StairUp, new Color(0.65f, 0.60f, 0.28f, 0.85f));
            Add(TileVisualKind.StairDown, new Color(0.42f, 0.34f, 0.12f, 0.85f));
            Add(TileVisualKind.Facility, new Color(0.30f, 0.45f, 0.55f, 0.85f));
        }

        public Material Get(TileVisualKind kind)
        {
            if (!materials.TryGetValue(kind, out var material))
            {
                throw new InvalidOperationException($"Tile material is not registered. Kind={kind}");
            }

            return material;
        }

        public void Dispose()
        {
            foreach (var material in materials.Values)
            {
                WorldDebugMaterialFactory.Dispose(material);
            }

            materials.Clear();
        }

        void Add(TileVisualKind kind, Color color)
        {
            materials.Add(kind, WorldDebugMaterialFactory.Create(color));
        }
    }
}
