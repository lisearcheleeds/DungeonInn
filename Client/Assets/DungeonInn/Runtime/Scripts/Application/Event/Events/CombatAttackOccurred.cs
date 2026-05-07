using System;

namespace DungeonInn.Application.Event.Events
{
    public sealed class CombatAttackOccurred : IGameEvent
    {
        public Guid AttackerActorId { get; }
        public Guid TargetActorId { get; }
        public int Damage { get; }
        public int TargetRemainingHp { get; }

        public CombatAttackOccurred(
            Guid attackerActorId,
            Guid targetActorId,
            int damage,
            int targetRemainingHp)
        {
            AttackerActorId = attackerActorId;
            TargetActorId = targetActorId;
            Damage = Math.Max(0, damage);
            TargetRemainingHp = Math.Max(0, targetRemainingHp);
        }
    }
}
