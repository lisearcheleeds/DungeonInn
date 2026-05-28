using System;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using DungeonInn.Master;
using NUnit.Framework;

namespace DungeonInn.Tests.EditMode
{
    public sealed class ActorRecoverBehaviorTests
    {
        [Test]
        public void AdventurerRecoverReducesStressThroughBehaviorHook()
        {
            var behavior = new AdventurerBehavior(15, AdventurerLifecycleState.Exploring);
            var actor = CreateActor(behavior);

            actor.Recover(10, 5, 3, 7, 2);

            Assert.That(actor.Hp, Is.EqualTo(30));
            Assert.That(actor.Mp, Is.EqualTo(15));
            Assert.That(actor.Fatigue, Is.EqualTo(7));
            Assert.That(actor.InjurySeverity, Is.EqualTo(3));
            Assert.That(behavior.Stress, Is.EqualTo(8));
        }

        [Test]
        public void MonsterRecoverKeepsRecoveryAmountsAndHasNoBehaviorSpecificEffect()
        {
            var behavior = new MonsterBehavior(1);
            var actor = CreateActor(behavior);

            actor.Recover(10, 5, 3, 7, 2);

            Assert.That(actor.Hp, Is.EqualTo(30));
            Assert.That(actor.Mp, Is.EqualTo(15));
            Assert.That(actor.Fatigue, Is.EqualTo(7));
            Assert.That(actor.InjurySeverity, Is.EqualTo(3));
            Assert.That(behavior.SpeciesId, Is.EqualTo(1));
        }

        [Test]
        public void OtherRecoverBehaviorsUseNoOpHook()
        {
            var pet = CreateActor(new PetBehavior(Guid.NewGuid()));
            var guildStaff = CreateActor(new GuildStaffBehavior(Array.Empty<ItemStack>()));

            pet.Recover(10, 5, 3, 7, 2);
            guildStaff.Recover(10, 5, 3, 7, 2);

            AssertRecovered(pet);
            AssertRecovered(guildStaff);
        }

        static Actor CreateActor(IActorBehavior behavior)
        {
            return new Actor(
                Guid.NewGuid(),
                1,
                new ActorStats(5, 5, 5, 5, 5, 5),
                new Inventory(new FixedItemStackLimitResolver()),
                1,
                0,
                20,
                10,
                10,
                5,
                1,
                new LayerPosition(MapLayerId.Ground, 0f, 0f),
                new ActorFaction(1, "Test"),
                behavior,
                WeaponTypeCombatMasterCatalog.Get(WeaponType.Fist));
        }

        static void AssertRecovered(Actor actor)
        {
            Assert.That(actor.Hp, Is.EqualTo(30));
            Assert.That(actor.Mp, Is.EqualTo(15));
            Assert.That(actor.Fatigue, Is.EqualTo(7));
            Assert.That(actor.InjurySeverity, Is.EqualTo(3));
        }
    }
}


