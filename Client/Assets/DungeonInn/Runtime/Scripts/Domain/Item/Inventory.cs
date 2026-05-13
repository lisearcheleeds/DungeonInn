using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Domain.Common;

namespace DungeonInn.Domain.Item
{
    public sealed class Inventory : IReadOnlyInventory
    {
        readonly List<InventorySlot> slots = new();
        readonly int maxSlotCount;
        readonly IItemStackLimitResolver stackLimitResolver;
        readonly Dictionary<int, int> itemCountsCache = new();
        bool itemCountsCacheDirty = true;

        public IReadOnlyList<InventorySlot> Slots => slots;
        public IReadOnlyDictionary<int, int> ItemCounts
        {
            get
            {
                RebuildItemCountsCacheIfNeeded();
                return itemCountsCache;
            }
        }
        public int Gold => ItemCounts.TryGetValue(SpecialItemIds.Money, out var gold) ? gold : 0;
        public int UsedSlotCount => slots.Count;
        public int MaxSlotCount => maxSlotCount;
        public bool IsFull => maxSlotCount <= UsedSlotCount;

        public Inventory(IItemStackLimitResolver stackLimitResolver)
            : this(GameConstants.DefaultInventorySlotCapacity, stackLimitResolver)
        {
        }

        public Inventory(int maxSlotCount, IItemStackLimitResolver stackLimitResolver)
        {
            if (maxSlotCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSlotCount));
            }

            this.maxSlotCount = maxSlotCount;
            this.stackLimitResolver = stackLimitResolver ?? throw new ArgumentNullException(nameof(stackLimitResolver));
        }

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
            if (!CanAdd(itemStack))
            {
                throw new InvalidOperationException("Inventory slot capacity is full.");
            }

            AddInternal(itemStack);
        }

        public void AddRange(IEnumerable<ItemStack> itemStacks)
        {
            var stacks = itemStacks?.ToArray() ?? throw new ArgumentNullException(nameof(itemStacks));
            if (!CanAddAll(stacks))
            {
                throw new InvalidOperationException("Inventory slot capacity is full.");
            }

            foreach (var itemStack in stacks)
            {
                Add(itemStack);
            }
        }

        public void Remove(ItemStack itemStack)
        {
            if (!ItemCounts.TryGetValue(itemStack.ItemId, out var count))
            {
                throw new InvalidOperationException("Item does not exist.");
            }

            if (count < itemStack.Count)
            {
                throw new InvalidOperationException("Item count is not enough.");
            }

            var remaining = itemStack.Count;
            for (var slotIndex = slots.Count - 1; 0 <= slotIndex && 0 < remaining; slotIndex--)
            {
                var slot = slots[slotIndex];
                if (slot.ItemId != itemStack.ItemId)
                {
                    continue;
                }

                var removeCount = Math.Min(slot.Count, remaining);
                remaining -= removeCount;
                var nextCount = slot.Count - removeCount;
                if (nextCount == 0)
                {
                    slots.RemoveAt(slotIndex);
                    continue;
                }

                slots[slotIndex] = new InventorySlot(slot.ItemId, nextCount);
            }
            itemCountsCacheDirty = true;
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
            return ItemCounts.TryGetValue(itemStack.ItemId, out var count) && itemStack.Count <= count;
        }

        public bool CanAdd(ItemStack itemStack)
        {
            return CanAddAll(new[] { itemStack });
        }

        public bool CanAddAll(IEnumerable<ItemStack> itemStacks)
        {
            var simulatedSlots = slots.ToList();
            foreach (var itemStack in itemStacks ?? throw new ArgumentNullException(nameof(itemStacks)))
            {
                AddToSimulatedSlots(simulatedSlots, itemStack);
            }

            return simulatedSlots.Count <= maxSlotCount;
        }

        public bool CanAddAfterRemoving(IEnumerable<ItemStack> removingItemStacks, IEnumerable<ItemStack> addingItemStacks)
        {
            var simulatedSlots = slots.ToList();
            foreach (var itemStack in removingItemStacks ?? throw new ArgumentNullException(nameof(removingItemStacks)))
            {
                if (!RemoveFromSimulatedSlots(simulatedSlots, itemStack))
                {
                    return false;
                }
            }

            foreach (var itemStack in addingItemStacks ?? throw new ArgumentNullException(nameof(addingItemStacks)))
            {
                AddToSimulatedSlots(simulatedSlots, itemStack);
            }

            return simulatedSlots.Count <= maxSlotCount;
        }

        public bool HasAll(IEnumerable<ItemStack> itemStacks)
        {
            var requiredCounts = itemStacks
                .GroupBy(itemStack => itemStack.ItemId)
                .Select(group => new ItemStack(group.Key, group.Sum(itemStack => itemStack.Count)));

            return requiredCounts.All(Has);
        }

        void AddInternal(ItemStack itemStack)
        {
            var remaining = itemStack.Count;
            var maxStackCount = GetMaxStackCount(itemStack.ItemId);
            for (var slotIndex = 0; slotIndex < slots.Count && 0 < remaining; slotIndex++)
            {
                var slot = slots[slotIndex];
                if (slot.ItemId != itemStack.ItemId || maxStackCount <= slot.Count)
                {
                    continue;
                }

                var addCount = Math.Min(maxStackCount - slot.Count, remaining);
                slots[slotIndex] = new InventorySlot(slot.ItemId, slot.Count + addCount);
                remaining -= addCount;
            }

            while (0 < remaining)
            {
                var addCount = Math.Min(maxStackCount, remaining);
                slots.Add(new InventorySlot(itemStack.ItemId, addCount));
                remaining -= addCount;
            }

            itemCountsCacheDirty = true;
        }

        void AddToSimulatedSlots(List<InventorySlot> simulatedSlots, ItemStack itemStack)
        {
            var remaining = itemStack.Count;
            var maxStackCount = GetMaxStackCount(itemStack.ItemId);
            for (var slotIndex = 0; slotIndex < simulatedSlots.Count && 0 < remaining; slotIndex++)
            {
                var slot = simulatedSlots[slotIndex];
                if (slot.ItemId != itemStack.ItemId || maxStackCount <= slot.Count)
                {
                    continue;
                }

                var addCount = Math.Min(maxStackCount - slot.Count, remaining);
                simulatedSlots[slotIndex] = new InventorySlot(slot.ItemId, slot.Count + addCount);
                remaining -= addCount;
            }

            while (0 < remaining)
            {
                var addCount = Math.Min(maxStackCount, remaining);
                simulatedSlots.Add(new InventorySlot(itemStack.ItemId, addCount));
                remaining -= addCount;
            }
        }

        static bool RemoveFromSimulatedSlots(List<InventorySlot> simulatedSlots, ItemStack itemStack)
        {
            var totalCount = simulatedSlots
                .Where(slot => slot.ItemId == itemStack.ItemId)
                .Sum(slot => slot.Count);
            if (totalCount < itemStack.Count)
            {
                return false;
            }

            var remaining = itemStack.Count;
            for (var slotIndex = simulatedSlots.Count - 1; 0 <= slotIndex && 0 < remaining; slotIndex--)
            {
                var slot = simulatedSlots[slotIndex];
                if (slot.ItemId != itemStack.ItemId)
                {
                    continue;
                }

                var removeCount = Math.Min(slot.Count, remaining);
                remaining -= removeCount;
                var nextCount = slot.Count - removeCount;
                if (nextCount == 0)
                {
                    simulatedSlots.RemoveAt(slotIndex);
                    continue;
                }

                simulatedSlots[slotIndex] = new InventorySlot(slot.ItemId, nextCount);
            }

            return true;
        }

        int GetMaxStackCount(int itemId)
        {
            var maxStackCount = stackLimitResolver.GetMaxStackCount(itemId);
            if (maxStackCount < 1)
            {
                throw new InvalidOperationException("Item stack limit must be greater than zero.");
            }

            return maxStackCount;
        }

        void RebuildItemCountsCacheIfNeeded()
        {
            if (!itemCountsCacheDirty)
            {
                return;
            }

            itemCountsCache.Clear();
            foreach (var slot in slots)
            {
                if (itemCountsCache.TryGetValue(slot.ItemId, out var count))
                {
                    itemCountsCache[slot.ItemId] = count + slot.Count;
                    continue;
                }

                itemCountsCache.Add(slot.ItemId, slot.Count);
            }

            itemCountsCacheDirty = false;
        }
    }
}
