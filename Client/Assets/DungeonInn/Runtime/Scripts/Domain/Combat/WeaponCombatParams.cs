using System;

namespace DungeonInn.Domain.Combat
{
    public sealed class WeaponCombatParams
    {
        public int AttackPower { get; }
        public float RangeMeters { get; }
        public float AttackIntervalSeconds { get; }
        public WeaponAttackDefinition AttackDefinition { get; }

        public WeaponCombatParams(
            int attackPower,
            float rangeMeters,
            float attackIntervalSeconds,
            WeaponAttackDefinition attackDefinition)
        {
            AttackPower = Math.Max(0, attackPower);
            RangeMeters = Math.Max(0, rangeMeters);
            AttackIntervalSeconds = Math.Max(0, attackIntervalSeconds);
            AttackDefinition = attackDefinition ?? throw new ArgumentNullException(nameof(attackDefinition));
        }
    }
}
