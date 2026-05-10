using System;

namespace DungeonInn.Application.Event.Events
{
    public sealed class ProjectileFired : IGameEvent
    {
        public Guid ProjectileId { get; }
        public Guid AttackerActorId { get; }
        public Guid TargetActorId { get; }

        public ProjectileFired(Guid projectileId, Guid attackerActorId, Guid targetActorId)
        {
            ProjectileId = projectileId;
            AttackerActorId = attackerActorId;
            TargetActorId = targetActorId;
        }
    }
}
