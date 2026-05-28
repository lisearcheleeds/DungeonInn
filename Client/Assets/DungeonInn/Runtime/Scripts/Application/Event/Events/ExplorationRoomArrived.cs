using System;

namespace DungeonInn.Application.Event.Events
{
    public sealed class ExplorationRoomArrived : IGameEvent
    {
        public Guid ActorId { get; }

        public ExplorationRoomArrived(Guid actorId)
        {
            ActorId = actorId;
        }
    }
}
