using System;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.Factory
{
    public sealed class MonsterCreateRequest
    {
        public int SpeciesId { get; }
        public Guid ActorId { get; }
        public LayerPosition Position { get; }
        public ActorFaction Faction { get; }
        public int PreferenceSeed { get; }

        public MonsterCreateRequest(
            int speciesId,
            Guid actorId,
            LayerPosition position,
            ActorFaction faction,
            int preferenceSeed)
        {
            if (speciesId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(speciesId));
            }

            SpeciesId = speciesId;
            ActorId = actorId;
            Position = position;
            Faction = faction ?? throw new ArgumentNullException(nameof(faction));
            PreferenceSeed = preferenceSeed;
        }
    }
}
