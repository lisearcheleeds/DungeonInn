using System;

namespace DungeonInn.Application.Profiles
{
    public interface IActorProfileRegistry
    {
        void Register(Guid actorId, string displayName);
        void RegisterMonster(Guid actorId, string displayName, int monsterSpeciesId);
        bool TryGetProfile(Guid actorId, out ActorProfile profile);
    }
}
