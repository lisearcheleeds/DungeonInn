using System;
using DungeonInn.Domain.Item;

namespace DungeonInn.Master
{
    public sealed class WeaponMaster
    {
        public int ItemId { get; }
        public WeaponType WeaponType { get; }
        public WeaponTypeCombatMaster WeaponTypeCombatMaster { get; }
        public int Attack { get; }
        public float RangeModifierMeters { get; }
        public float AttackIntervalModifierSeconds { get; }

        public WeaponMaster(
            int itemId,
            WeaponType weaponType,
            WeaponTypeCombatMaster weaponTypeCombatMaster,
            int attack,
            float rangeModifierMeters,
            float attackIntervalModifierSeconds)
        {
            if (itemId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(itemId));
            }

            if (weaponType == WeaponType.None)
            {
                throw new ArgumentException("Weapon type is required.", nameof(weaponType));
            }

            ItemId = itemId;
            WeaponType = weaponType;
            WeaponTypeCombatMaster = weaponTypeCombatMaster ?? throw new ArgumentNullException(nameof(weaponTypeCombatMaster));
            Attack = Math.Max(0, attack);
            RangeModifierMeters = rangeModifierMeters;
            AttackIntervalModifierSeconds = attackIntervalModifierSeconds;
        }
    }
}
