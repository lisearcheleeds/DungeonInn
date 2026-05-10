using System;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Combat;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using DungeonInn.Master;
using NUnit.Framework;

namespace DungeonInn.Tests.EditMode
{
    public sealed class WeaponCombatCalculatorTests
    {
        [TestCase(WeaponType.Sword)]
        [TestCase(WeaponType.Axe)]
        [TestCase(WeaponType.Fist)]
        [TestCase(WeaponType.Claws)]
        [TestCase(WeaponType.Fangs)]
        public void CloseRangeWeaponTypesUseDirectDamageAttackSpec(WeaponType weaponType)
        {
            var actor = CreateActor();
            EquipWeapon(actor, 100 + (int)weaponType, weaponType, 10, 0, 0);

            Assert.That(actor.WeaponCombatParams.AttackSpec.Nodes.Count, Is.EqualTo(1));
            Assert.That(actor.WeaponCombatParams.AttackSpec.Nodes[0].Type, Is.EqualTo(CombatEffectNodeType.DirectDamage));
            Assert.That(actor.WeaponCombatParams.AttackSpec.Nodes[0].DamageSpec.Amount, Is.EqualTo(actor.WeaponAttack));
            Assert.DoesNotThrow(() => new DungeonInn.Domain.Combat.CombatEffectGraphValidator().Validate(actor.WeaponCombatParams.AttackSpec));
        }

        [Test]
        public void ScytheUsesDurationAreaAttackSpecLinkedToDirectDamage()
        {
            var actor = CreateActor();
            actor.ChangeNaturalWeaponType(WeaponType.Scythe);

            var attackSpec = actor.WeaponCombatParams.AttackSpec;
            Assert.That(attackSpec.Nodes.Count, Is.EqualTo(2));
            Assert.That(attackSpec.RootNodeIds.Count, Is.EqualTo(1));
            Assert.That(attackSpec.RootNodeIds[0], Is.EqualTo(1));
            Assert.That(attackSpec.Nodes[0].Type, Is.EqualTo(CombatEffectNodeType.Area));
            Assert.That(attackSpec.Nodes[0].AreaSpec.Shape, Is.EqualTo(AttackAreaShape.Circle));
            Assert.That(attackSpec.Nodes[0].AreaSpec.DurationType, Is.EqualTo(AttackAreaDurationType.Duration));
            Assert.That(attackSpec.Nodes[0].AreaSpec.HitIntervalType, Is.EqualTo(AttackHitIntervalType.OncePerTarget));
            Assert.That(attackSpec.Nodes[0].Links.Count, Is.EqualTo(1));
            Assert.That(attackSpec.Nodes[0].Links[0].TriggerType, Is.EqualTo(CombatEffectTriggerType.OnHit));
            Assert.That(attackSpec.Nodes[0].Links[0].TargetNodeId, Is.EqualTo(2));
            Assert.That(attackSpec.Nodes[1].Type, Is.EqualTo(CombatEffectNodeType.DirectDamage));
            Assert.That(attackSpec.Nodes[1].DamageSpec.Amount, Is.EqualTo(actor.WeaponAttack));
            Assert.DoesNotThrow(() => new CombatEffectGraphValidator().Validate(attackSpec));
        }

        [Test]
        public void BowUsesProjectileAttackSpecLinkedToDirectDamage()
        {
            var actor = CreateActor();
            EquipWeapon(actor, 102, WeaponType.Bow, 10, 0, 0);

            var attackSpec = actor.WeaponCombatParams.AttackSpec;
            Assert.That(attackSpec.Nodes.Count, Is.EqualTo(2));
            Assert.That(attackSpec.RootNodeIds.Count, Is.EqualTo(1));
            Assert.That(attackSpec.RootNodeIds[0], Is.EqualTo(1));
            Assert.That(attackSpec.Nodes[0].Type, Is.EqualTo(CombatEffectNodeType.Projectile));
            Assert.That(attackSpec.Nodes[0].ProjectileSpec.SpeedMetersPerSecond, Is.GreaterThan(0f));
            Assert.That(attackSpec.Nodes[0].ProjectileSpec.MaxDistanceMeters, Is.EqualTo(actor.WeaponCombatParams.RangeMeters));
            Assert.That(attackSpec.Nodes[0].Links.Count, Is.EqualTo(1));
            Assert.That(attackSpec.Nodes[0].Links[0].TriggerType, Is.EqualTo(CombatEffectTriggerType.OnHit));
            Assert.That(attackSpec.Nodes[0].Links[0].TargetNodeId, Is.EqualTo(2));
            Assert.That(attackSpec.Nodes[1].Type, Is.EqualTo(CombatEffectNodeType.DirectDamage));
            Assert.That(attackSpec.Nodes[1].DamageSpec.Amount, Is.EqualTo(actor.WeaponAttack));
            Assert.DoesNotThrow(() => new CombatEffectGraphValidator().Validate(attackSpec));
        }

        [Test]
        public void ActorStartsWithFistDirectCombatParams()
        {
            var actor = CreateActor();

            Assert.That(actor.NaturalWeaponType, Is.EqualTo(WeaponType.Fist));
            Assert.That(actor.WeaponCombatParams.AttackSpec.Nodes.Count, Is.EqualTo(1));
            Assert.That(actor.WeaponCombatParams.AttackSpec.Nodes[0].Type, Is.EqualTo(CombatEffectNodeType.DirectDamage));
            Assert.That(actor.WeaponCombatParams.AttackSpec.Nodes[0].DamageSpec.Amount, Is.EqualTo(actor.WeaponAttack));
            Assert.That(actor.WeaponCombatParams.RangeMeters, Is.EqualTo(1.5f));
            Assert.That(actor.WeaponCombatParams.AttackIntervalSeconds, Is.EqualTo(1.0f));
        }

        [Test]
        public void NaturalWeaponTypeIsUsedWhenNoWeaponIsEquipped()
        {
            var actor = CreateActor();

            actor.ChangeNaturalWeaponType(WeaponType.Claws);

            Assert.That(actor.WeaponCombatParams.RangeMeters, Is.EqualTo(1.5f));
            Assert.That(actor.WeaponCombatParams.AttackIntervalSeconds, Is.EqualTo(0.9f));
            Assert.That(actor.WeaponCombatParams.AttackSpec.Nodes[0].DamageSpec.Amount, Is.EqualTo(actor.WeaponAttack));
        }

        [Test]
        public void EquippingDifferentWeaponTypeRefreshesCombatRangeAndAttackInterval()
        {
            var actor = CreateActor();

            EquipWeapon(actor, 101, WeaponType.Sword, 10, 0, 0);
            Assert.That(actor.WeaponCombatParams.RangeMeters, Is.EqualTo(2.0f));
            Assert.That(actor.WeaponCombatParams.AttackIntervalSeconds, Is.EqualTo(1.2f));

            EquipWeapon(actor, 102, WeaponType.Bow, 10, 0, 0);
            Assert.That(actor.WeaponCombatParams.RangeMeters, Is.EqualTo(20.0f));
            Assert.That(actor.WeaponCombatParams.AttackIntervalSeconds, Is.EqualTo(1.5f));
        }

        [Test]
        public void WeaponMasterModifiersAreAppliedToWeaponTypeCombatBaseValues()
        {
            var actor = CreateActor();

            EquipWeapon(actor, 101, WeaponType.Sword, 10, 0.5f, -0.2f);

            Assert.That(actor.WeaponCombatParams.RangeMeters, Is.EqualTo(2.5f));
            Assert.That(actor.WeaponCombatParams.AttackIntervalSeconds, Is.EqualTo(1.0f));
        }

        [Test]
        public void IncreasingStatsRefreshesDirectDamageAmount()
        {
            var actor = CreateActor();
            EquipWeapon(actor, 101, WeaponType.Sword, 10, 0, 0);
            var previousAttack = actor.WeaponCombatParams.AttackSpec.Nodes[0].DamageSpec.Amount;

            actor.IncreaseStats(3, 0, 0, 0, 0, 0);

            Assert.That(actor.WeaponAttack, Is.GreaterThan(previousAttack));
            Assert.That(actor.WeaponCombatParams.AttackSpec.Nodes[0].DamageSpec.Amount, Is.EqualTo(actor.WeaponAttack));
        }

        [Test]
        public void UnequippingWeaponReturnsToFistCombatParams()
        {
            var actor = CreateActor();

            EquipWeapon(actor, 102, WeaponType.Bow, 10, 0, 0);
            actor.Unequip(EquipmentSlot.Weapon);

            Assert.That(actor.WeaponCombatParams.RangeMeters, Is.EqualTo(1.5f));
            Assert.That(actor.WeaponCombatParams.AttackIntervalSeconds, Is.EqualTo(1.0f));
            Assert.That(actor.WeaponCombatParams.AttackSpec.Nodes[0].DamageSpec.Amount, Is.EqualTo(actor.WeaponAttack));
        }

        [Test]
        public void EquippingArmorDoesNotChangeCurrentWeaponCombatRange()
        {
            var actor = CreateActor();

            EquipWeapon(actor, 101, WeaponType.Sword, 10, 0, 0);
            var weaponRange = actor.WeaponCombatParams.RangeMeters;
            actor.Equip(new EquipmentMaster(201, EquipmentSlot.Armor, 5));

            Assert.That(actor.WeaponCombatParams.RangeMeters, Is.EqualTo(weaponRange));
        }

        static Actor CreateActor()
        {
            return new Actor(
                Guid.NewGuid(),
                0,
                new ActorStats(5, 5, 5, 5, 5, 5),
                new Inventory(new FixedItemStackLimitResolver()),
                1,
                0,
                50,
                10,
                0,
                0,
                1,
                new LayerPosition(MapLayerId.Ground, 0, 0),
                new ActorFaction(1, "Test"),
                new AdventurerBehavior(0));
        }

        static void EquipWeapon(
            Actor actor,
            int itemId,
            WeaponType weaponType,
            int attack,
            float rangeModifierMeters,
            float attackIntervalModifierSeconds)
        {
            actor.Equip(
                new EquipmentMaster(itemId, EquipmentSlot.Weapon, 0),
                new WeaponMaster(itemId, weaponType, attack, rangeModifierMeters, attackIntervalModifierSeconds));
        }
    }
}

