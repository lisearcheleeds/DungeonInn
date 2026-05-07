using System;

namespace DungeonInn.Application.Profiles
{
    public interface IActorProfileRegistry
    {
        void Register(Guid actorId, string displayName);
        bool TryGetProfile(Guid actorId, out ActorProfile profile);
    }
}
