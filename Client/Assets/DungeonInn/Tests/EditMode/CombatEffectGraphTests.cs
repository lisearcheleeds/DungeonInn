using System;
using DungeonInn.Domain.Combat;
using NUnit.Framework;

namespace DungeonInn.Tests.EditMode
{
    public sealed class CombatEffectGraphTests
    {
        [Test]
        public void ValidatorAcceptsComplexProjectileAreaDirectGraph()
        {
            var directDamage = new CombatEffectNodeSpec(
                3,
                CombatEffectNodeType.DirectDamage,
                new DamageSpec(10),
                null,
                null,
                Array.Empty<CombatEffectLinkSpec>());
            var area = new CombatEffectNodeSpec(
                2,
                CombatEffectNodeType.Area,
                null,
                new AttackAreaSpec(
                    AttackAreaShape.Circle,
                    AttackAreaDurationType.Instant,
                    AttackHitIntervalType.OncePerTarget,
                    0,
                    0,
                    5,
                    0,
                    0),
                null,
                new[] { new CombatEffectLinkSpec(CombatEffectTriggerType.OnHit, directDamage.Id) });
            var projectile = new CombatEffectNodeSpec(
                1,
                CombatEffectNodeType.Projectile,
                null,
                null,
                new ProjectileSpec(
                    ProjectileMovementType.TargetPoint,
                    ProjectileHitBehavior.DisappearOnHit,
                    10,
                    20,
                    null),
                new[]
                {
                    new CombatEffectLinkSpec(CombatEffectTriggerType.OnHit, area.Id),
                    new CombatEffectLinkSpec(CombatEffectTriggerType.OnExpired, area.Id)
                });
            var attackSpec = new WeaponAttackSpec(
                1,
                new[] { projectile.Id },
                new[] { projectile, area, directDamage },
                4);

            Assert.DoesNotThrow(() => new CombatEffectGraphValidator().Validate(attackSpec));
        }

        [Test]
        public void ValidatorAcceptsOnTickFromDurationArea()
        {
            var directDamage = CreateDirectDamageNode(2);
            var area = new CombatEffectNodeSpec(
                1,
                CombatEffectNodeType.Area,
                null,
                new AttackAreaSpec(
                    AttackAreaShape.Circle,
                    AttackAreaDurationType.Duration,
                    AttackHitIntervalType.EverySecond,
                    0,
                    0,
                    3,
                    0,
                    60),
                null,
                new[] { new CombatEffectLinkSpec(CombatEffectTriggerType.OnTick, directDamage.Id) });
            var attackSpec = new WeaponAttackSpec(
                1,
                new[] { area.Id },
                new[] { area, directDamage },
                4);

            Assert.DoesNotThrow(() => new CombatEffectGraphValidator().Validate(attackSpec));
        }

        [Test]
        public void ValidatorRejectsCombatEffectCycles()
        {
            var first = new CombatEffectNodeSpec(
                1,
                CombatEffectNodeType.Projectile,
                null,
                null,
                new ProjectileSpec(
                    ProjectileMovementType.Direction,
                    ProjectileHitBehavior.Pierce,
                    10,
                    20,
                    null),
                new[] { new CombatEffectLinkSpec(CombatEffectTriggerType.OnHit, 2) });
            var second = new CombatEffectNodeSpec(
                2,
                CombatEffectNodeType.Area,
                null,
                new AttackAreaSpec(
                    AttackAreaShape.Circle,
                    AttackAreaDurationType.Instant,
                    AttackHitIntervalType.OncePerTarget,
                    0,
                    0,
                    3,
                    0,
                    0),
                null,
                new[] { new CombatEffectLinkSpec(CombatEffectTriggerType.OnCompleted, 1) });
            var attackSpec = new WeaponAttackSpec(
                1,
                new[] { first.Id },
                new[] { first, second },
                4);

            Assert.Throws<InvalidOperationException>(() => new CombatEffectGraphValidator().Validate(attackSpec));
        }

        [Test]
        public void ValidatorRejectsDisconnectedCombatEffectCycles()
        {
            var root = CreateDirectDamageNode(1);
            var cycleStart = new CombatEffectNodeSpec(
                2,
                CombatEffectNodeType.Projectile,
                null,
                null,
                new ProjectileSpec(
                    ProjectileMovementType.Direction,
                    ProjectileHitBehavior.Pierce,
                    10,
                    20,
                    null),
                new[] { new CombatEffectLinkSpec(CombatEffectTriggerType.OnHit, 3) });
            var cycleEnd = new CombatEffectNodeSpec(
                3,
                CombatEffectNodeType.Area,
                null,
                new AttackAreaSpec(
                    AttackAreaShape.Circle,
                    AttackAreaDurationType.Instant,
                    AttackHitIntervalType.OncePerTarget,
                    0,
                    0,
                    3,
                    0,
                    0),
                null,
                new[] { new CombatEffectLinkSpec(CombatEffectTriggerType.OnCompleted, 2) });
            var attackSpec = new WeaponAttackSpec(
                1,
                new[] { root.Id },
                new[] { root, cycleStart, cycleEnd },
                4);

            Assert.Throws<InvalidOperationException>(() => new CombatEffectGraphValidator().Validate(attackSpec));
        }

        [Test]
        public void ValidatorRejectsDuplicatedNodeIds()
        {
            var first = CreateDirectDamageNode(1);
            var second = CreateDirectDamageNode(1);
            var attackSpec = new WeaponAttackSpec(
                1,
                new[] { first.Id },
                new[] { first, second },
                4);

            Assert.Throws<InvalidOperationException>(() => new CombatEffectGraphValidator().Validate(attackSpec));
        }

        [Test]
        public void ValidatorRejectsMissingRootNode()
        {
            var node = CreateDirectDamageNode(1);
            var attackSpec = new WeaponAttackSpec(
                1,
                new[] { 999 },
                new[] { node },
                4);

            Assert.Throws<InvalidOperationException>(() => new CombatEffectGraphValidator().Validate(attackSpec));
        }

        [Test]
        public void ValidatorRejectsMissingLinkTargetNode()
        {
            var node = new CombatEffectNodeSpec(
                1,
                CombatEffectNodeType.Projectile,
                null,
                null,
                new ProjectileSpec(
                    ProjectileMovementType.Direction,
                    ProjectileHitBehavior.DisappearOnHit,
                    10,
                    20,
                    null),
                new[] { new CombatEffectLinkSpec(CombatEffectTriggerType.OnHit, 999) });
            var attackSpec = new WeaponAttackSpec(
                1,
                new[] { node.Id },
                new[] { node },
                4);

            Assert.Throws<InvalidOperationException>(() => new CombatEffectGraphValidator().Validate(attackSpec));
        }

        [Test]
        public void ValidatorRejectsOnTickFromInstantArea()
        {
            var directDamage = CreateDirectDamageNode(2);
            var area = new CombatEffectNodeSpec(
                1,
                CombatEffectNodeType.Area,
                null,
                new AttackAreaSpec(
                    AttackAreaShape.Circle,
                    AttackAreaDurationType.Instant,
                    AttackHitIntervalType.OncePerTarget,
                    0,
                    0,
                    3,
                    0,
                    0),
                null,
                new[] { new CombatEffectLinkSpec(CombatEffectTriggerType.OnTick, directDamage.Id) });
            var attackSpec = new WeaponAttackSpec(
                1,
                new[] { area.Id },
                new[] { area, directDamage },
                4);

            Assert.Throws<InvalidOperationException>(() => new CombatEffectGraphValidator().Validate(attackSpec));
        }

        [Test]
        public void CombatEffectNodeSpecRequiresSpecForNodeType()
        {
            Assert.Throws<ArgumentException>(() => new CombatEffectNodeSpec(
                1,
                CombatEffectNodeType.DirectDamage,
                null,
                null,
                null,
                Array.Empty<CombatEffectLinkSpec>()));
            Assert.Throws<ArgumentException>(() => new CombatEffectNodeSpec(
                1,
                CombatEffectNodeType.Area,
                null,
                null,
                null,
                Array.Empty<CombatEffectLinkSpec>()));
            Assert.Throws<ArgumentException>(() => new CombatEffectNodeSpec(
                1,
                CombatEffectNodeType.Projectile,
                null,
                null,
                null,
                Array.Empty<CombatEffectLinkSpec>()));
        }

        [Test]
        public void WeaponAttackSpecRejectsInvalidMaxGenerationDepth()
        {
            var node = CreateDirectDamageNode(1);

            Assert.Throws<ArgumentOutOfRangeException>(() => new WeaponAttackSpec(
                1,
                new[] { node.Id },
                new[] { node },
                0));
        }

        static CombatEffectNodeSpec CreateDirectDamageNode(int id)
        {
            return new CombatEffectNodeSpec(
                id,
                CombatEffectNodeType.DirectDamage,
                new DamageSpec(10),
                null,
                null,
                Array.Empty<CombatEffectLinkSpec>());
        }
    }
}
