using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Domain.Item;

namespace DungeonInn.Master
{
    public sealed class MarketOfferMaster
    {
        public MarketOfferMaster(
            int id,
            int requiredGuildTotalLevel,
            int displayPriority,
            IReadOnlyList<ItemStack> requirements,
            IReadOnlyList<ItemStack> rewards)
        {
            if (id < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            Id = id;
            RequiredGuildTotalLevel = Math.Max(0, requiredGuildTotalLevel);
            DisplayPriority = displayPriority;
            Requirements = (requirements ?? Array.Empty<ItemStack>()).ToArray();
            Rewards = (rewards ?? Array.Empty<ItemStack>()).ToArray();
        }

        public int Id { get; }
        public int RequiredGuildTotalLevel { get; }
        public int DisplayPriority { get; }
        public IReadOnlyList<ItemStack> Requirements { get; }
        public IReadOnlyList<ItemStack> Rewards { get; }
    }
}
