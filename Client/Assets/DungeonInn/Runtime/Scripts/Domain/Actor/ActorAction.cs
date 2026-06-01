using System;
using DungeonInn.Domain.Map;

namespace DungeonInn.Domain.Actor
{
    public sealed class ActorAction
    {
        public ActorActionType Type { get; }
        public ActorActionState State { get; private set; }
        public int TargetId { get; }
        public bool HasTargetPosition { get; }
        public LayerPosition TargetPosition { get; }
        public float ArrivalRadiusMeters { get; }
        public Guid? SubTypeId { get; }

        public ActorAction(
            ActorActionType type,
            ActorActionState state,
            int targetId,
            bool hasTargetPosition,
            LayerPosition targetPosition)
            : this(type, state, targetId, hasTargetPosition, targetPosition, null)
        {
        }

        public ActorAction(
            ActorActionType type,
            ActorActionState state,
            int targetId,
            bool hasTargetPosition,
            LayerPosition targetPosition,
            Guid? subTypeId)
            : this(type, state, targetId, hasTargetPosition, targetPosition, 0f, subTypeId)
        {
        }

        public ActorAction(
            ActorActionType type,
            ActorActionState state,
            int targetId,
            bool hasTargetPosition,
            LayerPosition targetPosition,
            float arrivalRadiusMeters,
            Guid? subTypeId)
        {
            Type = type;
            State = state;
            TargetId = Math.Max(0, targetId);
            HasTargetPosition = hasTargetPosition;
            TargetPosition = targetPosition;
            ArrivalRadiusMeters = Math.Max(0f, arrivalRadiusMeters);
            SubTypeId = subTypeId;
        }

        public static ActorAction None()
        {
            return new ActorAction(ActorActionType.None, ActorActionState.Completed, 0, false, default);
        }

        public static ActorAction Wait()
        {
            return new ActorAction(ActorActionType.Wait, ActorActionState.NotStarted, 0, false, default);
        }

        public static ActorAction MoveTo(LayerPosition targetPosition)
        {
            return new ActorAction(ActorActionType.Move, ActorActionState.NotStarted, 0, true, targetPosition);
        }

        public static ActorAction MoveTo(LayerPosition targetPosition, float arrivalRadiusMeters)
        {
            return new ActorAction(
                ActorActionType.Move,
                ActorActionState.NotStarted,
                0,
                true,
                targetPosition,
                arrivalRadiusMeters,
                null);
        }

        public static ActorAction Attack(int targetId)
        {
            return Attack(targetId, null);
        }

        public static ActorAction Attack(int targetId, Guid? subTypeId)
        {
            return new ActorAction(ActorActionType.Attack, ActorActionState.NotStarted, targetId, false, default, subTypeId);
        }

        public void Start()
        {
            if (State != ActorActionState.NotStarted)
            {
                throw new InvalidOperationException("Actor action cannot be started.");
            }

            State = ActorActionState.Running;
        }

        public void Complete()
        {
            if (State != ActorActionState.Running && State != ActorActionState.NotStarted)
            {
                throw new InvalidOperationException("Actor action cannot be completed.");
            }

            State = ActorActionState.Completed;
        }

        public void Fail()
        {
            if (State == ActorActionState.Completed || State == ActorActionState.Cancelled)
            {
                throw new InvalidOperationException("Actor action cannot be failed.");
            }

            State = ActorActionState.Failed;
        }

        public void Cancel()
        {
            if (State == ActorActionState.Completed || State == ActorActionState.Failed)
            {
                throw new InvalidOperationException("Actor action cannot be cancelled.");
            }

            State = ActorActionState.Cancelled;
        }
    }
}
