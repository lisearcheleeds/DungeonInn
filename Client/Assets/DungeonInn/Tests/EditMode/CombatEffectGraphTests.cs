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
            var directDamage = new CombatEffectNode(
                3,
                CombatEffectNodeType.DirectDamage,
                new DamageSpec(10),
                null,
                null,
                Array.Empty<CombatEffectLink>());
            var area = new CombatEffectNode(
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
                new[] { new CombatEffectLink(CombatEffectTriggerType.OnHit, directDamage.Id) });
            var projectile = new CombatEffectNode(
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
                    new CombatEffectLink(CombatEffectTriggerType.OnHit, area.Id),
                    new CombatEffectLink(CombatEffectTriggerType.OnExpired, area.Id)
                });
            var definition = new WeaponAttackDefinition(
                1,
                new[] { projectile.Id },
                new[] { projectile, area, directDamage },
                4);

            Assert.DoesNotThrow(() => new CombatEffectGraphValidator().Validate(definition));
        }

        [Test]
        public void ValidatorAcceptsOnTickFromDurationArea()
        {
            var directDamage = CreateDirectDamageNode(2);
            var area = new CombatEffectNode(
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
                new[] { new CombatEffectLink(CombatEffectTriggerType.OnTick, directDamage.Id) });
            var definition = new WeaponAttackDefinition(
                1,
                new[] { area.Id },
                new[] { area, directDamage },
                4);

            Assert.DoesNotThrow(() => new CombatEffectGraphValidator().Validate(definition));
        }

        [Test]
        public void ValidatorRejectsCombatEffectCycles()
        {
            var first = new CombatEffectNode(
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
                new[] { new CombatEffectLink(CombatEffectTriggerType.OnHit, 2) });
            var second = new CombatEffectNode(
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
                new[] { new CombatEffectLink(CombatEffectTriggerType.OnCompleted, 1) });
            var definition = new WeaponAttackDefinition(
                1,
                new[] { first.Id },
                new[] { first, second },
                4);

            Assert.Throws<InvalidOperationException>(() => new CombatEffectGraphValidator().Validate(definition));
        }

        [Test]
        public void ValidatorRejectsDisconnectedCombatEffectCycles()
        {
            var root = CreateDirectDamageNode(1);
            var cycleStart = new CombatEffectNode(
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
                new[] { new CombatEffectLink(CombatEffectTriggerType.OnHit, 3) });
            var cycleEnd = new CombatEffectNode(
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
                new[] { new CombatEffectLink(CombatEffectTriggerType.OnCompleted, 2) });
            var definition = new WeaponAttackDefinition(
                1,
                new[] { root.Id },
                new[] { root, cycleStart, cycleEnd },
                4);

            Assert.Throws<InvalidOperationException>(() => new CombatEffectGraphValidator().Validate(definition));
        }

        [Test]
        public void ValidatorRejectsDuplicatedNodeIds()
        {
            var first = CreateDirectDamageNode(1);
            var second = CreateDirectDamageNode(1);
            var definition = new WeaponAttackDefinition(
                1,
                new[] { first.Id },
                new[] { first, second },
                4);

            Assert.Throws<InvalidOperationException>(() => new CombatEffectGraphValidator().Validate(definition));
        }

        [Test]
        public void ValidatorRejectsMissingRootNode()
        {
            var node = CreateDirectDamageNode(1);
            var definition = new WeaponAttackDefinition(
                1,
                new[] { 999 },
                new[] { node },
                4);

            Assert.Throws<InvalidOperationException>(() => new CombatEffectGraphValidator().Validate(definition));
        }

        [Test]
        public void ValidatorRejectsMissingLinkTargetNode()
        {
            var node = new CombatEffectNode(
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
                new[] { new CombatEffectLink(CombatEffectTriggerType.OnHit, 999) });
            var definition = new WeaponAttackDefinition(
                1,
                new[] { node.Id },
                new[] { node },
                4);

            Assert.Throws<InvalidOperationException>(() => new CombatEffectGraphValidator().Validate(definition));
        }

        [Test]
        public void ValidatorRejectsOnTickFromInstantArea()
        {
            var directDamage = CreateDirectDamageNode(2);
            var area = new CombatEffectNode(
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
                new[] { new CombatEffectLink(CombatEffectTriggerType.OnTick, directDamage.Id) });
            var definition = new WeaponAttackDefinition(
                1,
                new[] { area.Id },
                new[] { area, directDamage },
                4);

            Assert.Throws<InvalidOperationException>(() => new CombatEffectGraphValidator().Validate(definition));
        }

        [Test]
        public void CombatEffectNodeRequiresSpecForNodeType()
        {
            Assert.Throws<ArgumentException>(() => new CombatEffectNode(
                1,
                CombatEffectNodeType.DirectDamage,
                null,
                null,
                null,
                Array.Empty<CombatEffectLink>()));
            Assert.Throws<ArgumentException>(() => new CombatEffectNode(
                1,
                CombatEffectNodeType.Area,
                null,
                null,
                null,
                Array.Empty<CombatEffectLink>()));
            Assert.Throws<ArgumentException>(() => new CombatEffectNode(
                1,
                CombatEffectNodeType.Projectile,
                null,
                null,
                null,
                Array.Empty<CombatEffectLink>()));
        }

        [Test]
        public void WeaponAttackDefinitionRejectsInvalidMaxGenerationDepth()
        {
            var node = CreateDirectDamageNode(1);

            Assert.Throws<ArgumentOutOfRangeException>(() => new WeaponAttackDefinition(
                1,
                new[] { node.Id },
                new[] { node },
                0));
        }

        static CombatEffectNode CreateDirectDamageNode(int id)
        {
            return new CombatEffectNode(
                id,
                CombatEffectNodeType.DirectDamage,
                new DamageSpec(10),
                null,
                null,
                Array.Empty<CombatEffectLink>());
        }
    }
}
