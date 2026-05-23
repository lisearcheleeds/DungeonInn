using System;

namespace DungeonInn.Domain.Combat
{
    public sealed class ProjectileSpec
    {
        public ProjectileMovementType MovementType { get; }
        public ProjectileHitBehavior HitBehavior { get; }
        public float SpeedMetersPerSecond { get; }
        public float MaxDistanceMeters { get; }
        public AttackAreaSpec AreaSpec { get; }
        public string PrefabAddress { get; }

        public ProjectileSpec(
            ProjectileMovementType movementType,
            ProjectileHitBehavior hitBehavior,
            float speedMetersPerSecond,
            float maxDistanceMeters,
            AttackAreaSpec areaSpec,
            string prefabAddress = "")
        {
            MovementType = movementType;
            HitBehavior = hitBehavior;
            SpeedMetersPerSecond = Math.Max(0, speedMetersPerSecond);
            MaxDistanceMeters = Math.Max(0, maxDistanceMeters);
            AreaSpec = areaSpec;
            PrefabAddress = prefabAddress ?? string.Empty;
        }
    }
}
