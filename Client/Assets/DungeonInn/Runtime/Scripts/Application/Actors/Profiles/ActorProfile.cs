using System;
using DungeonInn.Domain.Actor;

namespace DungeonInn.Application.Actors.Profiles
{
    public sealed class ActorProfile
    {
        public Guid ActorId { get; }
        public string DisplayName { get; }
        public int ArchetypeId { get; }
        public int SpeciesId { get; }
        public ActorBehaviorType BehaviorType { get; }
        public int AdventurerSpawnMasterId { get; }

        public ActorProfile(
            Guid actorId,
            string displayName,
            int archetypeId,
            int speciesId,
            ActorBehaviorType behaviorType,
            int adventurerSpawnMasterId)
        {
            ActorId = actorId;
            DisplayName = displayName ?? string.Empty;
            ArchetypeId = Math.Max(0, archetypeId);
            SpeciesId = Math.Max(0, speciesId);
            BehaviorType = behaviorType;
            AdventurerSpawnMasterId = Math.Max(0, adventurerSpawnMasterId);
        }
    }
}
