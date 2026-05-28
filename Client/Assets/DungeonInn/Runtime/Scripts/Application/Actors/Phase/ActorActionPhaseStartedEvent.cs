using System;
using DungeonInn.Application.Event;
using DungeonInn.Domain.Actor;

namespace DungeonInn.Application.Actors.Phase
{
    public sealed class ActorActionPhaseStartedEvent : IGameEvent
    {
        public Guid ActorId { get; }
        public ActorActionType ActionType { get; }
        public Guid? SubTypeId { get; }
        public ActorActionPhaseName PhaseName { get; }

        public ActorActionPhaseStartedEvent(
            Guid actorId,
            ActorActionType actionType,
            Guid? subTypeId,
            ActorActionPhaseName phaseName)
        {
            ActorId = actorId;
            ActionType = actionType;
            SubTypeId = subTypeId;
            PhaseName = phaseName;
        }
    }
}
