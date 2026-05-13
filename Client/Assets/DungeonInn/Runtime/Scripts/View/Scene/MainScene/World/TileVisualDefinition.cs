using System;
using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class TileVisualDefinition
    {
        public TileVisualDefinition(
            TileVisualKind kind,
            TileMeshShapeKind shapeKind,
            Material material,
            Color fallbackColor)
        {
            Kind = kind;
            ShapeKind = shapeKind;
            Material = material;
            FallbackColor = fallbackColor;
        }

        public TileVisualKind Kind { get; }
        public TileMeshShapeKind ShapeKind { get; }
        public Material Material { get; }
        public Color FallbackColor { get; }

        public Material RequireMaterial()
        {
            if (Material == null)
            {
                throw new InvalidOperationException($"Tile material is not set. Kind={Kind}");
            }

            return Material;
        }
    }
}
