using System;
using DungeonInn.Domain.Actor;

namespace DungeonInn.Application.Actors.Profiles
{
    public interface IActorProfileRegistry
    {
        void Register(
            Guid actorId,
            string displayName,
            int archetypeId,
            int speciesId,
            ActorBehaviorType behaviorType,
            int adventurerSpawnMasterId);
        bool TryGetProfile(Guid actorId, out ActorProfile profile);
    }
}
