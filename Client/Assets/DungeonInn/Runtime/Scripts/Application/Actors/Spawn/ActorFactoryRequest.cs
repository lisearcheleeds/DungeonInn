using System;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.Actors.Spawn
{
    public sealed class ActorFactoryRequest
    {
        public int ArchetypeId { get; }
        public Guid ActorId { get; }
        public LayerPosition Position { get; }
        public ActorFaction Faction { get; }
        public int PreferenceSeed { get; }
        public ActorBehaviorType RequiredBehaviorType { get; }
        public string DisplayName { get; }
        public int AdventurerSpawnMasterId { get; }

        public ActorFactoryRequest(
            int archetypeId,
            Guid actorId,
            LayerPosition position,
            ActorFaction faction,
            int preferenceSeed,
            ActorBehaviorType requiredBehaviorType,
            string displayName,
            int adventurerSpawnMasterId)
        {
            if (archetypeId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(archetypeId));
            }

            ArchetypeId = archetypeId;
            ActorId = actorId;
            Position = position;
            Faction = faction ?? throw new ArgumentNullException(nameof(faction));
            PreferenceSeed = preferenceSeed;
            RequiredBehaviorType = requiredBehaviorType;
            DisplayName = displayName ?? string.Empty;
            AdventurerSpawnMasterId = Math.Max(0, adventurerSpawnMasterId);
        }
    }
}
