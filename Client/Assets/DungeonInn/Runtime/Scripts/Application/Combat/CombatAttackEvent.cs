using System;

namespace DungeonInn.Application.Combat
{
    public sealed class CombatAttackEvent
    {
        public Guid AttackerActorId { get; }
        public string AttackerName { get; }
        public Guid TargetActorId { get; }
        public string TargetName { get; }
        public int Damage { get; }
        public int TargetRemainingHp { get; }

        public CombatAttackEvent(
            Guid attackerActorId,
            string attackerName,
            Guid targetActorId,
            string targetName,
            int damage,
            int targetRemainingHp)
        {
            AttackerActorId = attackerActorId;
            AttackerName = attackerName ?? string.Empty;
            TargetActorId = targetActorId;
            TargetName = targetName ?? string.Empty;
            Damage = Math.Max(0, damage);
            TargetRemainingHp = Math.Max(0, targetRemainingHp);
        }
    }
}
