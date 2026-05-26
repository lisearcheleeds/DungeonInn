using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class MapTileVisualConfig
    {
        readonly Dictionary<TileVisualKind, TileVisualDefinition> definitions = new();

        public MapTileVisualConfig(MapMaterialSet materialSet)
        {
            if (materialSet == null)
            {
                throw new ArgumentNullException(nameof(materialSet));
            }

            Add(TileVisualKind.GroundWalkable, TileMeshShapeKind.Plane, materialSet.Get(TileVisualKind.GroundWalkable), new Color(0.24f, 0.32f, 0.24f, 0.45f));
            Add(TileVisualKind.GroundBlocked, TileMeshShapeKind.Block, materialSet.Get(TileVisualKind.GroundBlocked), new Color(0.25f, 0.25f, 0.25f, 0.65f));
            Add(TileVisualKind.DungeonWalkable, TileMeshShapeKind.Plane, materialSet.Get(TileVisualKind.DungeonWalkable), new Color(0.18f, 0.20f, 0.26f, 0.65f));
            Add(TileVisualKind.DungeonBlocked, TileMeshShapeKind.Block, materialSet.Get(TileVisualKind.DungeonBlocked), new Color(0.10f, 0.10f, 0.12f, 0.75f));
            Add(TileVisualKind.StairUp, TileMeshShapeKind.Ramp, materialSet.Get(TileVisualKind.StairUp), new Color(0.65f, 0.60f, 0.28f, 0.85f));
            Add(TileVisualKind.StairDown, TileMeshShapeKind.Ramp, materialSet.Get(TileVisualKind.StairDown), new Color(0.42f, 0.34f, 0.12f, 0.85f));
            Add(TileVisualKind.Facility, TileMeshShapeKind.Marker, materialSet.Get(TileVisualKind.Facility), new Color(0.30f, 0.45f, 0.55f, 0.85f));
        }

        public TileVisualDefinition Get(TileVisualKind kind)
        {
            if (!definitions.TryGetValue(kind, out var definition))
            {
                throw new InvalidOperationException($"Tile visual definition is not registered. Kind={kind}");
            }

            return definition;
        }

        void Add(TileVisualKind kind, TileMeshShapeKind shapeKind, Material material, Color fallbackColor)
        {
            definitions.Add(kind, new TileVisualDefinition(kind, shapeKind, material, fallbackColor));
        }
    }
}
