using System;
using System.Collections.Generic;
using DungeonInn.Domain.Item;

namespace DungeonInn.Domain.Actor
{
    public sealed class ScoutCostPolicy
    {
        public IReadOnlyList<ItemStack> Calculate(Actor candidate)
        {
            if (candidate == null)
            {
                throw new ArgumentNullException(nameof(candidate));
            }

            var cost = Math.Max(1, candidate.Level * 10 + candidate.Stats.Charisma);
            return new[] { new ItemStack(SpecialItemIds.Money, cost) };
        }
    }
}
