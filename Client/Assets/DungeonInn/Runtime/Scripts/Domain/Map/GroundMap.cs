using System;

namespace DungeonInn.Domain.Map
{
    public sealed class GroundMap
    {
        readonly GroundCell[] cells;

        public MapLayer Layer { get; }
        public GridPosition DungeonEntrancePosition { get; }

        public GroundMap(MapLayer layer, GridPosition dungeonEntrancePosition, GroundCell[] cells)
        {
            Layer = layer ?? throw new ArgumentNullException(nameof(layer));
            DungeonEntrancePosition = dungeonEntrancePosition;
            this.cells = cells ?? throw new ArgumentNullException(nameof(cells));

            if (cells.Length != layer.Width * layer.Depth)
            {
                throw new ArgumentException("Ground cell count does not match layer size.", nameof(cells));
            }
        }

        public GroundCell GetCell(GridPosition position)
        {
            if (!Layer.Contains(position))
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            return cells[ToIndex(position)];
        }

        public bool IsWalkable(GridPosition position)
        {
            return Layer.Contains(position) && GetCell(position).BlockType == MapCellBlockType.Walkable;
        }

        public bool IsWalkable(LayerPosition position, float agentRadius)
        {
            return MapWalkability.IsWalkable(Layer, IsWalkable, position, agentRadius);
        }

        int ToIndex(GridPosition position)
        {
            return position.Z * Layer.Width + position.X;
        }
    }
}
