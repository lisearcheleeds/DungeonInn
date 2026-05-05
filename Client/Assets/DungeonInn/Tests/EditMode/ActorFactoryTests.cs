using System;
using DungeonInn.Application.Factory;
using DungeonInn.Application.UseCase;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using DungeonInn.Master;
using NUnit.Framework;

namespace DungeonInn.Tests.EditMode
{
    public sealed class ActorFactoryTests
    {
        [Test]
        public void AdventurerFactoryBuildsBareAdventurerFromArchetypeMaster()
        {
            var factory = new AdventurerFactory(new HardcodedMasterRepository());

            var actor = factory.Create(new AdventurerCreateRequest(
                1,
                Guid.NewGuid(),
                new LayerPosition(MapLayerId.Ground, 0, 0),
                new ActorFaction(1, "Adventurer"),
                123));

            Assert.That(actor.Name, Is.EqualTo("Novice Adventurer"));
            Assert.That(actor.RequireBehavior<AdventurerBehavior>(), Is.Not.Null);
            Assert.That(actor.Inventory.ItemCounts, Is.Empty);
            Assert.That(actor.Equipment.Weapon, Is.Null);
            Assert.That(actor.NaturalWeaponType, Is.EqualTo(WeaponType.Fist));
            Assert.That(actor.Hp, Is.EqualTo(actor.Params.MaxHp));
            Assert.That(actor.Mp, Is.EqualTo(actor.Params.MaxMp));
        }

        [Test]
        public void CreateMonsterBuildsMonsterFromSpeciesMaster()
        {
            var factory = new MonsterFactory(new HardcodedMasterRepository());

            var actor = factory.Create(new MonsterCreateRequest(
                1,
                Guid.NewGuid(),
                new LayerPosition(MapLayerId.DungeonFloor(1), 10, 10),
                new ActorFaction(2, "Monster"),
                456));

            var behavior = actor.RequireBehavior<MonsterBehavior>();

            Assert.That(actor.Name, Is.EqualTo("Goblin"));
            Assert.That(behavior.SpeciesId, Is.EqualTo(1));
            Assert.That(actor.NaturalWeaponType, Is.EqualTo(WeaponType.Claws));
            Assert.That(actor.Equipment.Weapon, Is.Null);
            Assert.That(actor.WeaponCombatParams.AttackIntervalSeconds, Is.EqualTo(0.9f));
        }

        [Test]
        public void SpawnAdventurerFromMasterUsesGuildInventoryForRookieEquipment()
        {
            var repository = new HardcodedMasterRepository();
            var useCase = new SpawnAdventurerFromMasterUseCase(
                new AdventurerFactory(repository),
                repository,
                new SpawnAdventurerUseCase());
            var guildInventory = new Inventory();
            guildInventory.Add(new ItemStack(3001, 1));
            guildInventory.Add(new ItemStack(3003, 1));
            var guild = new AdventurerGuild(Guid.NewGuid(), guildInventory, Array.Empty<DungeonInn.Domain.Facility.Facility>());

            var actor = useCase.ExecuteAsync(
                guild,
                new AdventurerCreateRequest(
                    1,
                    Guid.NewGuid(),
                    new LayerPosition(MapLayerId.Ground, 0, 0),
                    new ActorFaction(1, "Adventurer"),
                    123),
                10).GetAwaiter().GetResult();

            Assert.That(guild.Inventory.Has(new ItemStack(3001, 1)), Is.False);
            Assert.That(guild.Inventory.Has(new ItemStack(3003, 1)), Is.False);
            Assert.That(actor.Inventory.Has(new ItemStack(3001, 1)), Is.True);
            Assert.That(actor.Inventory.Has(new ItemStack(3003, 1)), Is.True);
            Assert.That(actor.Inventory.Has(new ItemStack(2001, 1)), Is.True);
            Assert.That(actor.Equipment.Weapon.WeaponType, Is.EqualTo(WeaponType.Sword));
            Assert.That(guild.Transactions.Count, Is.EqualTo(1));
        }
    }
}
