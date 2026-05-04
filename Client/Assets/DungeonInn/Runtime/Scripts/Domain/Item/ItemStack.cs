using System;

namespace DungeonInn.Domain.Item
{
    public readonly struct ItemStack
    {
        public int ItemId { get; }
        public int Count { get; }

        public ItemStack(int itemId, int count)
        {
            if (itemId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(itemId));
            }

            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            ItemId = itemId;
            Count = count;
        }
    }
}
