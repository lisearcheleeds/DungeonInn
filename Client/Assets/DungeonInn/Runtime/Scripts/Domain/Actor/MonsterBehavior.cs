using System;
using System.Collections.Generic;
using DungeonInn.Domain.Item;

namespace DungeonInn.Domain.Actor
{
    public sealed class MonsterBehavior : IActorBehavior, IActorDropSource
    {
        public int SpeciesId { get; }
        public IReadOnlyList<ActorDropEntry> DropTable { get; }

        public MonsterBehavior(int speciesId, IReadOnlyList<ActorDropEntry> dropTable)
        {
            if (speciesId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(speciesId));
            }

            SpeciesId = speciesId;
            DropTable = dropTable ?? throw new ArgumentNullException(nameof(dropTable));
        }
    }
}
