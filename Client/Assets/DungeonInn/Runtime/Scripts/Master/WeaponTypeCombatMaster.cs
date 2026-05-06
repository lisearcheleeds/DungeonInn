using System;
using DungeonInn.Domain.Item;

namespace DungeonInn.Master
{
    public sealed class WeaponTypeCombatMaster
    {
        public WeaponType WeaponType { get; }
        public float BaseRangeMeters { get; }
        public float BaseAttackIntervalSeconds { get; }

        public WeaponTypeCombatMaster(
            WeaponType weaponType,
            float baseRangeMeters,
            float baseAttackIntervalSeconds)
        {
            if (weaponType == WeaponType.None)
            {
                throw new ArgumentException("Weapon type is required.", nameof(weaponType));
            }

            WeaponType = weaponType;
            BaseRangeMeters = Math.Max(0, baseRangeMeters);
            BaseAttackIntervalSeconds = Math.Max(0, baseAttackIntervalSeconds);
        }
    }
}
