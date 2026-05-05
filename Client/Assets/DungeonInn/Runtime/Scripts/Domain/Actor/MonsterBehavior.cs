using System;
using System.Collections.Generic;
using DungeonInn.Domain.Item;

namespace DungeonInn.Domain.Actor
{
    public sealed class MonsterBehavior : IActorBehavior
    {
        public int SpeciesId { get; }
        public bool CanScavenge { get; }
        public IReadOnlyList<ItemStack> SpeciesDrops { get; }

        public MonsterBehavior(int speciesId, bool canScavenge, IReadOnlyList<ItemStack> speciesDrops)
        {
            if (speciesId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(speciesId));
            }

            SpeciesId = speciesId;
            CanScavenge = canScavenge;
            SpeciesDrops = speciesDrops ?? throw new ArgumentNullException(nameof(speciesDrops));
        }
    }
}
