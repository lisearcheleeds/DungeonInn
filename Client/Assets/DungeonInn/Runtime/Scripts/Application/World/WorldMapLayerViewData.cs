using System;
using System.Collections.Generic;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.World
{
    public readonly struct WorldMapLayerViewData
    {
        readonly WorldMapCellViewKind[] cellKinds;

        public WorldMapLayerViewData(
            MapLayerId layerId,
            string layerName,
            int width,
            int height,
            float cellSizeMeters,
            IReadOnlyList<WorldMapCellViewKind> cellKinds)
        {
            if (width < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            if (cellSizeMeters <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(cellSizeMeters));
            }

            LayerId = layerId;
            LayerName = layerName ?? throw new ArgumentNullException(nameof(layerName));
            if (cellKinds == null)
            {
                throw new ArgumentNullException(nameof(cellKinds));
            }

            if (cellKinds.Count != width * height)
            {
                throw new ArgumentException("Cell kind count must match layer size.", nameof(cellKinds));
            }

            Width = width;
            Height = height;
            CellSizeMeters = cellSizeMeters;
            this.cellKinds = new WorldMapCellViewKind[cellKinds.Count];
            for (var index = 0; index < cellKinds.Count; index++)
            {
                this.cellKinds[index] = cellKinds[index];
            }
        }

        public MapLayerId LayerId { get; }
        public string LayerName { get; }
        public int Width { get; }
        public int Height { get; }
        public float CellSizeMeters { get; }

        public WorldMapCellViewKind GetCellKind(GridPosition position)
        {
            if (position.X < 0 || Width <= position.X)
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            if (position.Z < 0 || Height <= position.Z)
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            return cellKinds[position.Z * Width + position.X];
        }

        public WorldMapCellViewKind GetCellKind(int x, int z)
        {
            return GetCellKind(new GridPosition(x, z));
        }
    }
}
