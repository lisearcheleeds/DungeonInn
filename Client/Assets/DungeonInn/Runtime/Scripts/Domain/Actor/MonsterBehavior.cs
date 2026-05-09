using System;
using System.Collections.Generic;
using DungeonInn.Domain.Item;

namespace DungeonInn.Domain.Actor
{
    public sealed class MonsterBehavior : IActorBehavior, IActorDropSource
    {
        public int SpeciesId { get; }
        public bool CanScavenge { get; }
        public IReadOnlyList<ActorDropEntry> DropTable { get; }

        public MonsterBehavior(int speciesId, bool canScavenge, IReadOnlyList<ActorDropEntry> dropTable)
        {
            if (speciesId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(speciesId));
            }

            SpeciesId = speciesId;
            CanScavenge = canScavenge;
            DropTable = dropTable ?? throw new ArgumentNullException(nameof(dropTable));
        }
    }
}
