using System;

namespace DungeonInn.Domain.Combat
{
    public sealed class WeaponCombatParams
    {
        public int AttackPower { get; }
        public float RangeMeters { get; }
        public float AttackIntervalSeconds { get; }
        public WeaponAttackSpec AttackSpec { get; }

        public WeaponCombatParams(
            int attackPower,
            float rangeMeters,
            float attackIntervalSeconds,
            WeaponAttackSpec attackSpec)
        {
            AttackPower = Math.Max(0, attackPower);
            RangeMeters = Math.Max(0, rangeMeters);
            AttackIntervalSeconds = Math.Max(0, attackIntervalSeconds);
            AttackSpec = attackSpec ?? throw new ArgumentNullException(nameof(attackSpec));
        }
    }
}
