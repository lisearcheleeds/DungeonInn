using System;
using DungeonInn.Master;

namespace DungeonInn.Domain.Combat
{
    public sealed class DirectWeaponCombatCalculator : IWeaponCombatCalculator
    {
        readonly float rangeMeters;
        readonly float attackIntervalSeconds;

        public DirectWeaponCombatCalculator(float rangeMeters, float attackIntervalSeconds)
        {
            this.rangeMeters = Math.Max(0, rangeMeters);
            this.attackIntervalSeconds = Math.Max(0, attackIntervalSeconds);
        }

        public WeaponCombatParams Calculate(IWeaponCombatSource source, WeaponMaster weaponMaster)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var attackPower = source.WeaponAttack;
            var attackSpec = CreateDirectAttackSpec(attackPower);
            var resolvedRangeMeters = weaponMaster == null ? rangeMeters : weaponMaster.RangeMeters;
            var resolvedAttackIntervalSeconds = weaponMaster == null ? attackIntervalSeconds : weaponMaster.AttackIntervalSeconds;
            return new WeaponCombatParams(attackPower, resolvedRangeMeters, resolvedAttackIntervalSeconds, attackSpec);
        }

        static WeaponAttackSpec CreateDirectAttackSpec(int attackPower)
        {
            var directDamageNode = new CombatEffectNodeSpec(
                1,
                CombatEffectNodeType.DirectDamage,
                new DamageSpec(attackPower),
                null,
                null,
                Array.Empty<CombatEffectLinkSpec>());

            return new WeaponAttackSpec(
                1,
                new[] { directDamageNode.Id },
                new[] { directDamageNode },
                1);
        }
    }
}
