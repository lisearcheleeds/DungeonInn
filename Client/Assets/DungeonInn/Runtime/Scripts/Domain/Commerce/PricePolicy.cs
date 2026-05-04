using System;
using System.Collections.Generic;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Item;

namespace DungeonInn.Domain.Commerce
{
    public sealed class PricePolicy
    {
        public ItemStack CalculateSalePrice(DungeonInn.Domain.Facility.Facility facility, IEnumerable<ItemStack> items, IReadOnlyDictionary<int, ItemDefinition> definitions)
        {
            return new ItemStack(SpecialItemIds.Money, CalculateItemTotal(items, definitions, facility.Level));
        }

        public ItemStack CalculatePurchasePrice(IEnumerable<ItemStack> items, IReadOnlyDictionary<int, ItemDefinition> definitions)
        {
            var total = CalculateItemTotal(items, definitions, 1);
            return new ItemStack(SpecialItemIds.Money, Math.Max(1, total / 2));
        }

        int CalculateItemTotal(IEnumerable<ItemStack> items, IReadOnlyDictionary<int, ItemDefinition> definitions, int multiplier)
        {
            var total = 0;

            foreach (var item in items)
            {
                if (!definitions.TryGetValue(item.ItemId, out var definition))
                {
                    throw new InvalidOperationException("Item definition does not exist.");
                }

                if (!definition.CanTrade)
                {
                    throw new InvalidOperationException("Item cannot be traded.");
                }

                total += definition.BasePrice * item.Count * Math.Max(1, multiplier);
            }

            return total;
        }
    }
}
