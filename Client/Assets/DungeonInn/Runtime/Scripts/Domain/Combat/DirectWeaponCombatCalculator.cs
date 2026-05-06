using System;
using DungeonInn.Master;

namespace DungeonInn.Domain.Combat
{
    public sealed class DirectWeaponCombatCalculator : IWeaponCombatCalculator
    {
        readonly WeaponTypeCombatMaster weaponTypeCombatMaster;

        public DirectWeaponCombatCalculator(WeaponTypeCombatMaster weaponTypeCombatMaster)
        {
            this.weaponTypeCombatMaster = weaponTypeCombatMaster
                ?? throw new ArgumentNullException(nameof(weaponTypeCombatMaster));
        }

        public WeaponCombatParams Calculate(IWeaponCombatSource source, WeaponMaster weaponMaster)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var attackPower = source.WeaponAttack;
            var attackSpec = CreateDirectAttackSpec(attackPower);
            var resolvedRangeMeters = weaponTypeCombatMaster.BaseRangeMeters
                + (weaponMaster == null ? 0f : weaponMaster.RangeModifierMeters);
            var resolvedAttackIntervalSeconds = weaponTypeCombatMaster.BaseAttackIntervalSeconds
                + (weaponMaster == null ? 0f : weaponMaster.AttackIntervalModifierSeconds);
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
