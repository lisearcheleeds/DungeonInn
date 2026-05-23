using System;
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
            var attackSpec = CreateAttackSpec(
                weaponTypeCombatMaster.WeaponType,
                attackPower,
                resolvedRangeMeters,
                weaponTypeCombatMaster.ProjectilePrefabAddress,
                weaponTypeCombatMaster.ProjectileSpeedMetersPerSecond,
                weaponTypeCombatMaster.AreaEffectPrefabAddress,
                weaponTypeCombatMaster.AreaEffectRadiusMeters,
                weaponTypeCombatMaster.AreaEffectDurationTicks);
            return new WeaponCombatParams(attackPower, resolvedRangeMeters, resolvedAttackIntervalSeconds, attackSpec);
        }

        static WeaponAttackSpec CreateAttackSpec(
            WeaponType weaponType,
            int attackPower,
            float resolvedRangeMeters,
            string projectilePrefabAddress,
            float projectileSpeedMetersPerSecond,
            string areaEffectPrefabAddress,
            float areaEffectRadiusMeters,
            int areaEffectDurationTicks)
        {
            switch (weaponType)
            {
                case WeaponType.Bow:
                    return CreateProjectileAttackSpec(
                        attackPower,
                        resolvedRangeMeters,
                        projectilePrefabAddress,
                        projectileSpeedMetersPerSecond);
                case WeaponType.Scythe:
                    return CreateAreaAttackSpec(
                        attackPower,
                        areaEffectPrefabAddress,
                        areaEffectRadiusMeters,
                        areaEffectDurationTicks);
                default:
                    return CreateDirectAttackSpec(attackPower);
            }
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

        static WeaponAttackSpec CreateProjectileAttackSpec(
            int attackPower,
            float maxDistanceMeters,
            string projectilePrefabAddress,
            float projectileSpeedMetersPerSecond)
        {
            var projectileNode = new CombatEffectNodeSpec(
                1,
                CombatEffectNodeType.Projectile,
                null,
                null,
                new ProjectileSpec(
                    ProjectileMovementType.TargetPoint,
                    ProjectileHitBehavior.DisappearOnHit,
                    projectileSpeedMetersPerSecond,
                    maxDistanceMeters,
                    null,
                    projectilePrefabAddress),
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

        static WeaponAttackSpec CreateAreaAttackSpec(
            int attackPower,
            string areaEffectPrefabAddress,
            float areaEffectRadiusMeters,
            int areaEffectDurationTicks)
        {
            var areaNode = new CombatEffectNodeSpec(
                1,
                CombatEffectNodeType.Area,
                null,
                new AttackAreaSpec(
                    AttackAreaShape.Circle,
                    AttackAreaDurationType.Duration,
                    AttackHitIntervalType.OncePerTarget,
                    0f,
                    0f,
                    areaEffectRadiusMeters,
                    0f,
                    areaEffectDurationTicks,
                    areaEffectPrefabAddress),
                null,
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
                3,
                new[] { areaNode.Id },
                new[] { areaNode, directDamageNode },
                2);
        }
    }
}
