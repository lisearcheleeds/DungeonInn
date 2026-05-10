using System;

namespace DungeonInn.Application.Event.Events
{
    public sealed class ActorWaitingForInn : IGameEvent
    {
        public Guid ActorId { get; }
        public Guid InnFacilityId { get; }

        public ActorWaitingForInn(Guid actorId, Guid innFacilityId)
        {
            ActorId = actorId;
            InnFacilityId = innFacilityId;
        }
    }
}
