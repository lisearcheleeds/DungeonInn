namespace DungeonInn.Domain.Item
{
    public interface IItemStackLimitResolver
    {
        int GetMaxStackCount(int itemId);
    }
}
