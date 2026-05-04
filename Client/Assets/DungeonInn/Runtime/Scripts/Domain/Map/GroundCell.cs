namespace DungeonInn.Domain.Map
{
    public sealed class GroundCell
    {
        public GridPosition Position { get; }
        public GroundCellType Type { get; private set; }
        public MapCellBlockType BlockType { get; private set; }

        public GroundCell(GridPosition position, GroundCellType type, MapCellBlockType blockType)
        {
            Position = position;
            Type = type;
            BlockType = blockType;
        }

        public void ChangeType(GroundCellType type, MapCellBlockType blockType)
        {
            Type = type;
            BlockType = blockType;
        }
    }
}
