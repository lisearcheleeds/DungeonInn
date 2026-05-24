using DungeonInn.Domain.Item;

namespace DungeonInn.Tests.EditMode
{
    public sealed class FixedItemStackLimitResolver : IItemStackLimitResolver
    {
        readonly int maxStackCount;

        public FixedItemStackLimitResolver()
            : this(int.MaxValue)
        {
        }

        public FixedItemStackLimitResolver(int maxStackCount)
        {
            this.maxStackCount = maxStackCount;
        }

        public int GetMaxStackCount(int itemId)
        {
            return maxStackCount;
        }
    }
}

