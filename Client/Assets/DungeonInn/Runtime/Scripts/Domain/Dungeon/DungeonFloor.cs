using System;
using System.Collections.Generic;
using DungeonInn.Domain.Map;

namespace DungeonInn.Domain.Dungeon
{
    public sealed class DungeonFloor
    {
        readonly DungeonCell[] cells;

        public int FloorIndex { get; }
        public MapLayer Layer { get; }
        public DungeonStair UpStair { get; }
        public DungeonStair DownStair { get; }
        public IReadOnlyList<DungeonRoom> Rooms { get; }
        public DungeonFloorGenerationSettings GenerationSettings { get; }

        public DungeonFloor(
            int floorIndex,
            MapLayer layer,
            DungeonCell[] cells,
            DungeonStair upStair,
            DungeonStair downStair,
            IReadOnlyList<DungeonRoom> rooms,
            DungeonFloorGenerationSettings generationSettings)
        {
            if (floorIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(floorIndex));
            }

            FloorIndex = floorIndex;
            Layer = layer ?? throw new ArgumentNullException(nameof(layer));
            this.cells = cells ?? throw new ArgumentNullException(nameof(cells));
            UpStair = upStair ?? throw new ArgumentNullException(nameof(upStair));
            DownStair = downStair ?? throw new ArgumentNullException(nameof(downStair));
            Rooms = rooms ?? throw new ArgumentNullException(nameof(rooms));
            GenerationSettings = generationSettings ?? throw new ArgumentNullException(nameof(generationSettings));

            if (cells.Length != layer.Width * layer.Depth)
            {
                throw new ArgumentException("Dungeon cell count does not match layer size.", nameof(cells));
            }
        }

        public DungeonCell GetCell(GridPosition position)
        {
            if (!Layer.Contains(position))
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            return cells[ToIndex(position)];
        }

        public bool IsWalkable(GridPosition position)
        {
            return Layer.Contains(position) && GetCell(position).IsWalkable;
        }

        public bool IsWalkable(LayerPosition position, float agentRadius)
        {
            return MapWalkability.IsWalkable(Layer, IsWalkable, position, agentRadius);
        }

        public bool IsStairPosition(GridPosition position, DungeonStairType stairType)
        {
            var stair = stairType == DungeonStairType.Up ? UpStair : DownStair;
            return stair.Position.Equals(position);
        }

        public LayerPosition GetArrivalPosition(DungeonStairType stairType)
        {
            var stair = stairType == DungeonStairType.Up ? UpStair : DownStair;
            return Layer.GetCellCenter(stair.Position);
        }

        int ToIndex(GridPosition position)
        {
            return position.Z * Layer.Width + position.X;
        }
    }
}
