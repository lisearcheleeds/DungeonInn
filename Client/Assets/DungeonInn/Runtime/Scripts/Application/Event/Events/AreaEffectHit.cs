using System;

namespace DungeonInn.Application.Event.Events
{
    public sealed class AreaEffectHit : IGameEvent
    {
        public Guid AreaEffectId { get; }
        public Guid AttackerActorId { get; }
        public Guid TargetActorId { get; }
        public int Damage { get; }

        public AreaEffectHit(Guid areaEffectId, Guid attackerActorId, Guid targetActorId, int damage)
        {
            AreaEffectId = areaEffectId;
            AttackerActorId = attackerActorId;
            TargetActorId = targetActorId;
            Damage = Math.Max(0, damage);
        }
    }
}
