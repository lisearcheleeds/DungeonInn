using System;

namespace DungeonInn.Application.Combat
{
    public sealed class ActorCombatState
    {
        public Guid? TargetActorId { get; private set; }
        public bool HasTarget => TargetActorId.HasValue;

        public void SetTarget(Guid targetId)
        {
            TargetActorId = targetId;
        }

        public void ClearTarget()
        {
            TargetActorId = null;
        }
    }
}
