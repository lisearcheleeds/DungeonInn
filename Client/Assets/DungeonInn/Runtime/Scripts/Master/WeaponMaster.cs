using System;
using DungeonInn.Domain.Item;

namespace DungeonInn.Master
{
    public sealed class WeaponMaster
    {
        public int ItemId { get; }
        public WeaponType WeaponType { get; }
        public int Attack { get; }
        public float RangeMeters { get; }
        public float AttackIntervalSeconds { get; }

        public WeaponMaster(
            int itemId,
            WeaponType weaponType,
            int attack,
            float rangeMeters,
            float attackIntervalSeconds)
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
            Attack = Math.Max(0, attack);
            RangeMeters = Math.Max(0, rangeMeters);
            AttackIntervalSeconds = Math.Max(0, attackIntervalSeconds);
        }
    }
}
