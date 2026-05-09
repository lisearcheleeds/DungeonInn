using System;
using System.Collections.Generic;

namespace DungeonInn.Application.Profiles
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

        public void RegisterMonster(Guid actorId, string displayName, int monsterSpeciesId)
        {
            if (!profiles.ContainsKey(actorId))
            {
                profiles[actorId] = new ActorProfile(actorId, displayName, monsterSpeciesId);
            }
        }

        public bool TryGetProfile(Guid actorId, out ActorProfile profile)
        {
            return profiles.TryGetValue(actorId, out profile);
        }
    }
}
