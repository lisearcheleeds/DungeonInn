using System;
using System.Collections.Generic;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Item;
using DungeonInn.Master;

namespace DungeonInn.Domain.Commerce
{
    public sealed class PricePolicy
    {
        public ItemStack CalculateSalePrice(DungeonInn.Domain.Facility.Facility facility, IEnumerable<ItemStack> items, IReadOnlyDictionary<int, ItemMaster> itemMasters)
        {
            return new ItemStack(SpecialItemIds.Money, CalculateItemTotal(items, itemMasters, facility.Level));
        }

        public ItemStack CalculatePurchasePrice(IEnumerable<ItemStack> items, IReadOnlyDictionary<int, ItemMaster> itemMasters)
        {
            var total = CalculateItemTotal(items, itemMasters, 1);
            return new ItemStack(SpecialItemIds.Money, Math.Max(1, total / 2));
        }

        public ItemStack CalculatePurchasePrice(ItemStack item, IReadOnlyDictionary<int, ItemMaster> itemMasters)
        {
            var total = CalculateItemTotal(item, itemMasters, 1);
            return new ItemStack(SpecialItemIds.Money, Math.Max(1, total / 2));
        }

        int CalculateItemTotal(IEnumerable<ItemStack> items, IReadOnlyDictionary<int, ItemMaster> itemMasters, int multiplier)
        {
            var total = 0;

            foreach (var item in items)
            {
                if (!itemMasters.TryGetValue(item.ItemId, out var itemMaster))
                {
                    throw new InvalidOperationException("Item master does not exist.");
                }

                if (!itemMaster.CanTrade)
                {
                    throw new InvalidOperationException("Item cannot be traded.");
                }

                total += itemMaster.BasePrice * item.Count * Math.Max(1, multiplier);
            }

            return total;
        }

        int CalculateItemTotal(ItemStack item, IReadOnlyDictionary<int, ItemMaster> itemMasters, int multiplier)
        {
            if (!itemMasters.TryGetValue(item.ItemId, out var itemMaster))
            {
                throw new InvalidOperationException("Item master does not exist.");
            }

            if (!itemMaster.CanTrade)
            {
                throw new InvalidOperationException("Item cannot be traded.");
            }

            return itemMaster.BasePrice * item.Count * Math.Max(1, multiplier);
        }
    }
}
