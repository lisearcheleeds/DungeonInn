namespace DungeonInn.Domain.Map
{
    public interface IGridWalkability
    {
        bool IsWalkable(GridPosition position);
    }
}
