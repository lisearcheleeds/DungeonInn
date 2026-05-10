using System;
using DungeonInn.Domain.Map;

namespace DungeonInn.Domain.Combat
{
    public sealed class ProjectileInstance
    {
        public Guid Id { get; }
        public Guid AttackerActorId { get; }
        public Guid TargetActorId { get; }
        public LayerPosition Position { get; private set; }
        public LayerPosition TargetPosition { get; }
        public WeaponAttackSpec AttackSpec { get; }
        public int SourceNodeId { get; }
        public CombatEffectExecutionId ExecutionId { get; }
        public int Damage { get; }
        public float SpeedMetersPerSecond { get; }
        public float RemainingDistanceMeters { get; private set; }

        public ProjectileInstance(
            Guid id,
            Guid attackerActorId,
            Guid targetActorId,
            LayerPosition position,
            LayerPosition targetPosition,
            int damage,
            float speedMetersPerSecond,
            float maxDistanceMeters)
            : this(
                id,
                attackerActorId,
                targetActorId,
                position,
                targetPosition,
                null,
                0,
                CombatEffectExecutionId.New(),
                damage,
                speedMetersPerSecond,
                maxDistanceMeters)
        {
        }

        public ProjectileInstance(
            Guid id,
            Guid attackerActorId,
            Guid targetActorId,
            LayerPosition position,
            LayerPosition targetPosition,
            WeaponAttackSpec attackSpec,
            int sourceNodeId,
            CombatEffectExecutionId executionId,
            int damage,
            float speedMetersPerSecond,
            float maxDistanceMeters)
        {
            if (!position.LayerId.Equals(targetPosition.LayerId))
            {
                throw new ArgumentException("Projectile target must be on the same layer.", nameof(targetPosition));
            }

            Id = id;
            AttackerActorId = attackerActorId;
            TargetActorId = targetActorId;
            Position = position;
            TargetPosition = targetPosition;
            AttackSpec = attackSpec;
            SourceNodeId = sourceNodeId;
            ExecutionId = executionId;
            Damage = Math.Max(0, damage);
            SpeedMetersPerSecond = Math.Max(0, speedMetersPerSecond);
            RemainingDistanceMeters = Math.Max(0, maxDistanceMeters);
        }

        public bool Advance(float deltaGameSeconds)
        {
            if (RemainingDistanceMeters <= 0f)
            {
                return false;
            }

            var dx = TargetPosition.X - Position.X;
            var dz = TargetPosition.Z - Position.Z;
            var distance = (float)Math.Sqrt(dx * dx + dz * dz);
            if (distance <= 0f)
            {
                RemainingDistanceMeters = 0f;
                return false;
            }

            var step = Math.Min(distance, Math.Min(RemainingDistanceMeters, SpeedMetersPerSecond * Math.Max(0f, deltaGameSeconds)));
            var ratio = step / distance;
            Position = new LayerPosition(
                Position.LayerId,
                Position.X + dx * ratio,
                Position.Z + dz * ratio);
            RemainingDistanceMeters -= step;
            return 0f < RemainingDistanceMeters && step < distance;
        }
    }
}
