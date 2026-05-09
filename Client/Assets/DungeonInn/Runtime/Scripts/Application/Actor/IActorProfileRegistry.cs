using System;
using DungeonInn.Domain.Actor;

namespace DungeonInn.Application.Profiles
{
    public interface IActorProfileRegistry
    {
        void Register(Guid actorId, string displayName);
        void Register(
            Guid actorId,
            string displayName,
            int archetypeId,
            int speciesId,
            ActorBehaviorType behaviorType);
        bool TryGetProfile(Guid actorId, out ActorProfile profile);
    }
}
