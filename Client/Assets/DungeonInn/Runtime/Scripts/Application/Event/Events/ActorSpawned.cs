using System;

namespace DungeonInn.Application.Event.Events
{
    public sealed class ActorSpawned : IGameEvent
    {
        public Guid ActorId { get; }

        public ActorSpawned(Guid actorId)
        {
            ActorId = actorId;
        }
    }
}
