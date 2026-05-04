using System;
using System.Collections.Generic;
using DungeonInn.Domain.Item;

namespace DungeonInn.Domain.Character
{
    public sealed class ScoutCost
    {
        public IReadOnlyList<ItemStack> Items { get; }

        public ScoutCost(IReadOnlyList<ItemStack> items)
        {
            Items = items ?? throw new ArgumentNullException(nameof(items));
        }
    }
}
