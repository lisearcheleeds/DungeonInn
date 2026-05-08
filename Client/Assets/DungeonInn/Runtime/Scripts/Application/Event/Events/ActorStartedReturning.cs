using System;

namespace DungeonInn.Application.Event.Events
{
    public sealed class ActorStartedReturning : IGameEvent
    {
        public Guid ActorId { get; }

        public ActorStartedReturning(Guid actorId)
        {
            ActorId = actorId;
        }
    }
}
