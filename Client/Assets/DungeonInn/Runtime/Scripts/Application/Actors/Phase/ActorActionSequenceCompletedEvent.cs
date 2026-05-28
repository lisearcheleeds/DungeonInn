using System;
using DungeonInn.Application.Event;
using DungeonInn.Domain.Actor;

namespace DungeonInn.Application.Actors.Phase
{
    public sealed class ActorActionSequenceCompletedEvent : IGameEvent
    {
        public Guid ActorId { get; }
        public ActorActionType ActionType { get; }
        public Guid? SubTypeId { get; }

        public ActorActionSequenceCompletedEvent(
            Guid actorId,
            ActorActionType actionType,
            Guid? subTypeId)
        {
            ActorId = actorId;
            ActionType = actionType;
            SubTypeId = subTypeId;
        }
    }
}
