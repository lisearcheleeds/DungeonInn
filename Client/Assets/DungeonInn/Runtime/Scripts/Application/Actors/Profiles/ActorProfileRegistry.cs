using System;
using System.Collections.Generic;
using DungeonInn.Domain.Actor;

namespace DungeonInn.Application.Actors.Profiles
{
    public sealed class ActorProfileRegistry : IActorProfileRegistry
    {
        readonly Dictionary<Guid, ActorProfile> profiles = new();

        public void Register(Guid actorId, string displayName)
        {
            if (!profiles.ContainsKey(actorId))
            {
                profiles[actorId] = new ActorProfile(actorId, displayName);
            }
        }

        public void Register(
            Guid actorId,
            string displayName,
            int archetypeId,
            int speciesId,
            ActorBehaviorType behaviorType)
        {
            if (!profiles.ContainsKey(actorId))
            {
                profiles[actorId] = new ActorProfile(actorId, displayName, archetypeId, speciesId, behaviorType);
            }
        }

        public bool TryGetProfile(Guid actorId, out ActorProfile profile)
        {
            return profiles.TryGetValue(actorId, out profile);
        }
    }
}
