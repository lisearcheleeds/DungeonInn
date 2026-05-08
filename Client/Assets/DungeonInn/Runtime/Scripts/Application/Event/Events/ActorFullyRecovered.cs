using System;

namespace DungeonInn.Application.Event.Events
{
    public sealed class ActorFullyRecovered : IGameEvent
    {
        public Guid ActorId { get; }

        public ActorFullyRecovered(Guid actorId)
        {
            ActorId = actorId;
        }
    }
}
