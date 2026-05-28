using System;
using DungeonInn.Domain.Actor;

namespace DungeonInn.Application.Actors.Phase
{
    public readonly struct ActorActionPhaseKey : IEquatable<ActorActionPhaseKey>
    {
        public ActorActionType ActionType { get; }
        public Guid? SubTypeId { get; }

        public ActorActionPhaseKey(ActorActionType actionType, Guid? subTypeId)
        {
            ActionType = actionType;
            SubTypeId = subTypeId;
        }

        public bool Equals(ActorActionPhaseKey other)
        {
            return ActionType == other.ActionType && SubTypeId.Equals(other.SubTypeId);
        }

        public override bool Equals(object obj)
        {
            return obj is ActorActionPhaseKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ActionType, SubTypeId);
        }

        public static bool operator ==(ActorActionPhaseKey left, ActorActionPhaseKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ActorActionPhaseKey left, ActorActionPhaseKey right)
        {
            return !left.Equals(right);
        }
    }
}
