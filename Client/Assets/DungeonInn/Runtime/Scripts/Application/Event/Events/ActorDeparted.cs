using System;

namespace DungeonInn.Application.Event.Events
{
    public sealed class ActorDeparted : IGameEvent
    {
        public Guid ActorId { get; }
        public int WaitedDays { get; }

        public ActorDeparted(Guid actorId, int waitedDays)
        {
            ActorId = actorId;
            WaitedDays = Math.Max(0, waitedDays);
        }
    }
}
