using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonInn.Domain.Item
{
    public sealed class Inventory
    {
        readonly Dictionary<int, int> itemCounts = new();

        public IReadOnlyDictionary<int, int> ItemCounts => itemCounts;
        public int Gold => itemCounts.TryGetValue(SpecialItemIds.Money, out var gold) ? gold : 0;

        public void AddGold(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Add(new ItemStack(SpecialItemIds.Money, amount));
        }

        public bool TrySpendGold(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (Gold < amount)
            {
                return false;
            }

            Remove(new ItemStack(SpecialItemIds.Money, amount));
            return true;
        }

        public void Add(ItemStack itemStack)
        {
            if (itemCounts.TryGetValue(itemStack.ItemId, out var count))
            {
                itemCounts[itemStack.ItemId] = count + itemStack.Count;
                return;
            }

            itemCounts.Add(itemStack.ItemId, itemStack.Count);
        }

        public void AddRange(IEnumerable<ItemStack> itemStacks)
        {
            foreach (var itemStack in itemStacks)
            {
                Add(itemStack);
            }
        }

        public void Remove(ItemStack itemStack)
        {
            if (!itemCounts.TryGetValue(itemStack.ItemId, out var count))
            {
                throw new InvalidOperationException("Item does not exist.");
            }

            if (count < itemStack.Count)
            {
                throw new InvalidOperationException("Item count is not enough.");
            }

            var nextCount = count - itemStack.Count;
            if (nextCount == 0)
            {
                itemCounts.Remove(itemStack.ItemId);
                return;
            }

            itemCounts[itemStack.ItemId] = nextCount;
        }

        public void RemoveRange(IEnumerable<ItemStack> itemStacks)
        {
            foreach (var itemStack in itemStacks)
            {
                Remove(itemStack);
            }
        }

        public bool Has(ItemStack itemStack)
        {
            return itemCounts.TryGetValue(itemStack.ItemId, out var count) && itemStack.Count <= count;
        }

        public bool HasAll(IEnumerable<ItemStack> itemStacks)
        {
            var requiredCounts = itemStacks
                .GroupBy(itemStack => itemStack.ItemId)
                .Select(group => new ItemStack(group.Key, group.Sum(itemStack => itemStack.Count)));

            return requiredCounts.All(Has);
        }
    }
}
