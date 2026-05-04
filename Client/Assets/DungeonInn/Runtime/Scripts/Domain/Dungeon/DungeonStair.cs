using DungeonInn.Domain.Map;

namespace DungeonInn.Domain.Dungeon
{
    public sealed class DungeonStair
    {
        public DungeonStairType Type { get; }
        public GridPosition Position { get; }

        public DungeonStair(DungeonStairType type, GridPosition position)
        {
            Type = type;
            Position = position;
        }
    }
}
