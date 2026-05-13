using System;
using System.Collections.Generic;
using DungeonInn.Domain.Item;

namespace DungeonInn.Domain.Commerce
{
    public interface IExchangeParticipant
    {
        Guid Id { get; }
        bool HasAll(IReadOnlyList<ItemStack> items);
        bool CanAddAfterRemoving(IReadOnlyList<ItemStack> toRemove, IReadOnlyList<ItemStack> toAdd);
        void RemoveRange(IReadOnlyList<ItemStack> items);
        void AddRange(IReadOnlyList<ItemStack> items);
    }
}
