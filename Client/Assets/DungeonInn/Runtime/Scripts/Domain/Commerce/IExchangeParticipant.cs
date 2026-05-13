using System;
using System.Collections.Generic;
using DungeonInn.Domain.Item;

namespace DungeonInn.Domain.Commerce
{
    public interface IExchangeParticipant
    {
        Guid Id { get; }
        bool Has(ItemStack item);
        bool HasAll(IReadOnlyList<ItemStack> items);
        bool CanAddAfterRemoving(ItemStack toRemove, ItemStack toAdd);
        bool CanAddAfterRemoving(IReadOnlyList<ItemStack> toRemove, IReadOnlyList<ItemStack> toAdd);
        void Remove(ItemStack item);
        void RemoveRange(IReadOnlyList<ItemStack> items);
        void Add(ItemStack item);
        void AddRange(IReadOnlyList<ItemStack> items);
    }
}
