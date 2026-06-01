using System;
using System.Collections.Generic;
using DungeonInn.Domain.Actor;

namespace DungeonInn.Application.Actors.Profiles
{
    /// <summary>
    /// Holds lifetime actor profile and spawn metadata only.
    /// Exploration achievements, combat counters, and frame-level transient state belong to dedicated state services.
    /// </summary>
    public sealed class ActorProfileRegistry : IActorProfileRegistry
    {
        readonly Dictionary<Guid, ActorProfile> profiles = new();

        public void Register(
            Guid actorId,
            string displayName,
            int archetypeId,
            int speciesId,
            ActorBehaviorType behaviorType,
            int adventurerSpawnMasterId)
        {
            if (!profiles.ContainsKey(actorId))
            {
                profiles[actorId] = new ActorProfile(
                    actorId,
                    displayName,
                    archetypeId,
                    speciesId,
                    behaviorType,
                    adventurerSpawnMasterId);
            }
        }

        public bool TryGetProfile(Guid actorId, out ActorProfile profile)
        {
            return profiles.TryGetValue(actorId, out profile);
        }
    }
}
