using System;

namespace DungeonInn.Application.Event.Events
{
    public sealed class InnSatisfactionChanged : IGameEvent
    {
        public Guid ActorId { get; }
        public int Delta { get; }
        public InnSatisfactionChangeReason Reason { get; }

        public InnSatisfactionChanged(Guid actorId, int delta, InnSatisfactionChangeReason reason)
        {
            ActorId = actorId;
            Delta = delta;
            Reason = reason;
        }
    }
}
