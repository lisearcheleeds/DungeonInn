using System;

namespace DungeonInn.Application.Event.Events
{
    public sealed class ActorReservedInn : IGameEvent
    {
        public Guid ActorId { get; }
        public Guid InnFacilityId { get; }

        public ActorReservedInn(Guid actorId, Guid innFacilityId)
        {
            ActorId = actorId;
            InnFacilityId = innFacilityId;
        }
    }
}
