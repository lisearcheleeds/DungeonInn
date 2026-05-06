using System;

namespace DungeonInn.Application.Combat
{
    public sealed class ActorCombatState
    {
        public Guid? TargetActorId { get; private set; }
        public bool HasTarget => TargetActorId.HasValue;
        public float NextAttackGameTimeSeconds { get; private set; }

        public void SetTarget(Guid targetId)
        {
            if (TargetActorId.HasValue && TargetActorId.Value.Equals(targetId))
            {
                return;
            }

            TargetActorId = targetId;
            NextAttackGameTimeSeconds = 0f;
        }

        public void ClearTarget()
        {
            TargetActorId = null;
            NextAttackGameTimeSeconds = 0f;
        }

        public bool IsAttackReady(float currentGameTimeSeconds)
        {
            return currentGameTimeSeconds >= NextAttackGameTimeSeconds;
        }

        public void RecordAttack(float currentGameTimeSeconds, float intervalSeconds)
        {
            NextAttackGameTimeSeconds = currentGameTimeSeconds + Math.Max(0f, intervalSeconds);
        }
    }
}
