using DungeonInn.Domain.Map;

namespace DungeonInn.Domain.Dungeon
{
    public sealed class DungeonCell
    {
        public GridPosition Position { get; }
        public DungeonCellType Type { get; private set; }
        public bool IsWalkable => Type == DungeonCellType.Corridor || Type == DungeonCellType.Room;

        public DungeonCell(GridPosition position, DungeonCellType type)
        {
            Position = position;
            Type = type;
        }

        public void ChangeType(DungeonCellType type)
        {
            Type = type;
        }
    }
}
