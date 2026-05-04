using System;

namespace DungeonInn.Domain.Map
{
    public sealed class MapLayer
    {
        public MapLayerId Id { get; }
        public int Width { get; }
        public int Depth { get; }
        public float CellSizeMeters { get; }

        public MapLayer(MapLayerId id, int width, int depth, float cellSizeMeters)
        {
            if (width < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (depth < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(depth));
            }

            if (cellSizeMeters <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(cellSizeMeters));
            }

            Id = id;
            Width = width;
            Depth = depth;
            CellSizeMeters = cellSizeMeters;
        }

        public bool Contains(GridPosition position)
        {
            return 0 <= position.X && position.X < Width && 0 <= position.Z && position.Z < Depth;
        }

        public bool Contains(LayerPosition position)
        {
            if (!Id.Equals(position.LayerId))
            {
                return false;
            }

            return 0f <= position.X
                && position.X < Width * CellSizeMeters
                && 0f <= position.Z
                && position.Z < Depth * CellSizeMeters;
        }

        public GridPosition ToGridPosition(LayerPosition position)
        {
            if (!Contains(position))
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            return new GridPosition(
                (int)Math.Floor(position.X / CellSizeMeters),
                (int)Math.Floor(position.Z / CellSizeMeters));
        }

        public LayerPosition GetCellCenter(GridPosition position)
        {
            if (!Contains(position))
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            return new LayerPosition(
                Id,
                (position.X + 0.5f) * CellSizeMeters,
                (position.Z + 0.5f) * CellSizeMeters);
        }
    }
}
