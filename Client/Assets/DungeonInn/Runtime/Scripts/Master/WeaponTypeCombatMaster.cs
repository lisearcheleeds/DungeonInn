using System;
using DungeonInn.Domain.Item;

namespace DungeonInn.Master
{
    public sealed class WeaponTypeCombatMaster
    {
        public WeaponType WeaponType { get; }
        public float BaseRangeMeters { get; }
        public float BaseAttackIntervalSeconds { get; }
        public string ProjectilePrefabAddress { get; }
        public string AreaEffectPrefabAddress { get; }
        public float ProjectileSpeedMetersPerSecond { get; }
        public float AreaEffectRadiusMeters { get; }
        public int AreaEffectDurationTicks { get; }

        public WeaponTypeCombatMaster(
            WeaponType weaponType,
            float baseRangeMeters,
            float baseAttackIntervalSeconds,
            string projectilePrefabAddress,
            string areaEffectPrefabAddress,
            float projectileSpeedMetersPerSecond,
            float areaEffectRadiusMeters,
            int areaEffectDurationTicks)
        {
            if (weaponType == WeaponType.None)
            {
                throw new ArgumentException("Weapon type is required.", nameof(weaponType));
            }

            WeaponType = weaponType;
            BaseRangeMeters = Math.Max(0, baseRangeMeters);
            BaseAttackIntervalSeconds = Math.Max(0, baseAttackIntervalSeconds);
            ProjectilePrefabAddress = projectilePrefabAddress ?? string.Empty;
            AreaEffectPrefabAddress = areaEffectPrefabAddress ?? string.Empty;
            ProjectileSpeedMetersPerSecond = Math.Max(0f, projectileSpeedMetersPerSecond);
            AreaEffectRadiusMeters = Math.Max(0f, areaEffectRadiusMeters);
            AreaEffectDurationTicks = Math.Max(1, areaEffectDurationTicks);
        }
    }
}
