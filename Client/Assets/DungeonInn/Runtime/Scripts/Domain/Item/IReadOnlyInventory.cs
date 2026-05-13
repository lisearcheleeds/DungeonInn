using System.Collections.Generic;

namespace DungeonInn.Domain.Item
{
    public interface IReadOnlyInventory
    {
        IReadOnlyList<InventorySlot> Slots { get; }
        IReadOnlyDictionary<int, int> ItemCounts { get; }
        int Gold { get; }
        int UsedSlotCount { get; }
        int MaxSlotCount { get; }
        bool IsFull { get; }

        bool Has(ItemStack itemStack);
        bool HasAll(IEnumerable<ItemStack> itemStacks);
        bool CanAdd(ItemStack itemStack);
        bool CanAddAll(IEnumerable<ItemStack> itemStacks);
        bool CanAddAfterRemoving(IEnumerable<ItemStack> removingItemStacks, IEnumerable<ItemStack> addingItemStacks);
    }
}
