using System;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.Factory
{
    public sealed class AdventurerCreateRequest
    {
        public int ArchetypeId { get; }
        public Guid ActorId { get; }
        public string DisplayName { get; }
        public LayerPosition Position { get; }
        public ActorFaction Faction { get; }
        public int PreferenceSeed { get; }

        public AdventurerCreateRequest(
            int archetypeId,
            Guid actorId,
            LayerPosition position,
            ActorFaction faction,
            int preferenceSeed)
            : this(archetypeId, actorId, position, faction, preferenceSeed, string.Empty)
        {
        }

        public AdventurerCreateRequest(
            int archetypeId,
            Guid actorId,
            LayerPosition position,
            ActorFaction faction,
            int preferenceSeed,
            string displayName)
        {
            if (archetypeId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(archetypeId));
            }

            ArchetypeId = archetypeId;
            ActorId = actorId;
            DisplayName = displayName ?? string.Empty;
            Position = position;
            Faction = faction ?? throw new ArgumentNullException(nameof(faction));
            PreferenceSeed = preferenceSeed;
        }
    }
}
