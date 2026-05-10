using System;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Item;
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

            var resolvedRangeMeters = weaponTypeCombatMaster.BaseRangeMeters
                + (weaponMaster == null ? 0f : weaponMaster.RangeModifierMeters);
            var resolvedAttackIntervalSeconds = weaponTypeCombatMaster.BaseAttackIntervalSeconds
                + (weaponMaster == null ? 0f : weaponMaster.AttackIntervalModifierSeconds);
            var attackPower = source.WeaponAttack;
            var attackSpec = weaponTypeCombatMaster.WeaponType == WeaponType.Bow
                ? CreateProjectileAttackSpec(attackPower, resolvedRangeMeters)
                : CreateDirectAttackSpec(attackPower);
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

        static WeaponAttackSpec CreateProjectileAttackSpec(int attackPower, float maxDistanceMeters)
        {
            var projectileNode = new CombatEffectNodeSpec(
                1,
                CombatEffectNodeType.Projectile,
                null,
                null,
                new ProjectileSpec(
                    ProjectileMovementType.TargetPoint,
                    ProjectileHitBehavior.DisappearOnHit,
                    GameConstants.ProjectileDefaultSpeedMetersPerSecond,
                    maxDistanceMeters,
                    null),
                new[]
                {
                    new CombatEffectLinkSpec(CombatEffectTriggerType.OnHit, 2)
                });
            var directDamageNode = new CombatEffectNodeSpec(
                2,
                CombatEffectNodeType.DirectDamage,
                new DamageSpec(attackPower),
                null,
                null,
                Array.Empty<CombatEffectLinkSpec>());

            return new WeaponAttackSpec(
                2,
                new[] { projectileNode.Id },
                new[] { projectileNode, directDamageNode },
                2);
        }
    }
}
