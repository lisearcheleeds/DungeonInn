using System;

namespace DungeonInn.Application.Event.Events
{
    public sealed class ProjectileHit : IGameEvent
    {
        public Guid ProjectileId { get; }
        public Guid AttackerActorId { get; }
        public Guid TargetActorId { get; }
        public int Damage { get; }

        public ProjectileHit(Guid projectileId, Guid attackerActorId, Guid targetActorId, int damage)
        {
            ProjectileId = projectileId;
            AttackerActorId = attackerActorId;
            TargetActorId = targetActorId;
            Damage = Math.Max(0, damage);
        }
    }
}
