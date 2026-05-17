using System;
using DungeonInn.Application.Event;
using DungeonInn.Application.Actors.Spawn;
using DungeonInn.Application.Actors.Profiles;
using DungeonInn.Application.Actors.Ai;
using DungeonInn.Application.Actors.Equipment;
using DungeonInn.Application.Actors.Lifecycle;
using DungeonInn.Application.Actors.Movement;


using DungeonInn.Application.Combat;
using DungeonInn.Application.Dungeons;
using DungeonInn.Application.Economy;
using DungeonInn.Application.Facilities;
using DungeonInn.Application.Items;
using DungeonInn.Application.World;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using DungeonInn.Master;
using NUnit.Framework;
using R3;

namespace DungeonInn.Tests.EditMode
{
    public sealed class ActorFactoryTests
    {
        [Test]
        public void ActorFactoryBuildsBareAdventurerFromArchetypeMaster()
        {
            var factory = new ActorFactory(new HardcodedMasterRepository());

            var actor = factory.Create(new ActorFactoryRequest(
                1,
                Guid.NewGuid(),
                new LayerPosition(MapLayerId.Ground, 0, 0),
                new ActorFaction(1, "Adventurer"),
                123,
                ActorBehaviorType.Adventurer,
                string.Empty));

            Assert.That(actor.RequireBehavior<AdventurerBehavior>(), Is.Not.Null);
            Assert.That(actor.Inventory.Gold, Is.EqualTo(100));
            Assert.That(actor.Inventory.Has(new ItemStack(2001, 1)), Is.True);
            Assert.That(actor.Equipment.EquippedWeaponType, Is.Null);
            Assert.That(actor.NaturalWeaponType, Is.EqualTo(WeaponType.Fist));
            Assert.That(actor.Hp, Is.EqualTo(actor.Params.MaxHp));
            Assert.That(actor.Mp, Is.EqualTo(actor.Params.MaxMp));
        }

        [Test]
        public void CreateMonsterBuildsMonsterFromArchetypeMaster()
        {
            var factory = new ActorFactory(new HardcodedMasterRepository());

            var actor = factory.Create(new ActorFactoryRequest(
                2,
                Guid.NewGuid(),
                new LayerPosition(MapLayerId.DungeonFloor(1), 10, 10),
                new ActorFaction(2, "Monster"),
                456,
                ActorBehaviorType.Monster,
                string.Empty));

            var behavior = actor.RequireBehavior<MonsterBehavior>();

            Assert.That(behavior.SpeciesId, Is.EqualTo(1));
            Assert.That(actor.NaturalWeaponType, Is.EqualTo(WeaponType.Claws));
            Assert.That(actor.Equipment.EquippedWeaponType, Is.Null);
            Assert.That(actor.WeaponCombatParams.AttackIntervalSeconds, Is.EqualTo(0.9f));
        }

        [Test]
        public void ActorFactoryBuildsAdventurerAndMonsterThroughCommonEntry()
        {
            var factory = new ActorFactory(new HardcodedMasterRepository());

            var adventurer = factory.Create(new ActorFactoryRequest(
                1,
                Guid.NewGuid(),
                new LayerPosition(MapLayerId.Ground, 0, 0),
                new ActorFaction(1, "Adventurer"),
                123,
                ActorBehaviorType.Adventurer,
                string.Empty));
            var monster = factory.Create(new ActorFactoryRequest(
                2,
                Guid.NewGuid(),
                new LayerPosition(MapLayerId.DungeonFloor(1), 10, 10),
                new ActorFaction(2, "Monster"),
                456,
                ActorBehaviorType.Monster,
                string.Empty));

            Assert.That(adventurer.RequireBehavior<AdventurerBehavior>(), Is.Not.Null);
            Assert.That(adventurer.Inventory.Has(new ItemStack(2001, 1)), Is.True);
            Assert.That(monster.RequireBehavior<MonsterBehavior>().SpeciesId, Is.EqualTo(1));
            Assert.That(monster.NaturalWeaponType, Is.EqualTo(WeaponType.Claws));
        }

        [Test]
        public void SpawnAdventurerUsesGuildInventoryForRookieEquipment()
        {
            var repository = new HardcodedMasterRepository();
            var profileRegistry = new NoOpActorProfileRegistry();
            var useCase = CreateSpawnAdventurerUseCase(repository, profileRegistry, new NoOpGameEventBus());
            var guildInventory = new Inventory(new FixedItemStackLimitResolver());
            guildInventory.Add(new ItemStack(3001, 1));
            guildInventory.Add(new ItemStack(3003, 1));
            var guild = new AdventurerGuild(Guid.NewGuid(), guildInventory, Array.Empty<DungeonInn.Domain.Facility.Facility>());

            var actor = useCase.ExecuteAsync(
                guild,
                new ActorFactoryRequest(
                    1,
                    Guid.NewGuid(),
                    new LayerPosition(MapLayerId.Ground, 0, 0),
                    new ActorFaction(1, "Adventurer"),
                    123,
                    ActorBehaviorType.Adventurer,
                    string.Empty),
                10).GetAwaiter().GetResult();

            Assert.That(guild.Inventory.Has(new ItemStack(3001, 1)), Is.False);
            Assert.That(guild.Inventory.Has(new ItemStack(3003, 1)), Is.False);
            Assert.That(actor.Inventory.Has(new ItemStack(3001, 1)), Is.False);
            Assert.That(actor.Inventory.Has(new ItemStack(3003, 1)), Is.False);
            Assert.That(actor.Inventory.Has(new ItemStack(2001, 1)), Is.True);
            Assert.That(actor.Equipment.EquippedWeaponType, Is.EqualTo(WeaponType.Sword));
            Assert.That(actor.Equipment.GetEquippedItemId(EquipmentSlot.Armor).HasValue, Is.True);
            Assert.That(guild.Transactions.Count, Is.EqualTo(1));
        }

        [Test]
        public void SpawnAdventurerRegistersRequestDisplayName()
        {
            var repository = new HardcodedMasterRepository();
            var profileRegistry = new RecordingActorProfileRegistry();
            var useCase = CreateSpawnAdventurerUseCase(repository, profileRegistry, new NoOpGameEventBus());
            var guildInventory = new Inventory(new FixedItemStackLimitResolver());
            guildInventory.Add(new ItemStack(3001, 1));
            guildInventory.Add(new ItemStack(3003, 1));
            var guild = new AdventurerGuild(Guid.NewGuid(), guildInventory, Array.Empty<DungeonInn.Domain.Facility.Facility>());
            var actorId = Guid.NewGuid();

            useCase.ExecuteAsync(
                guild,
                new ActorFactoryRequest(
                    1,
                    actorId,
                    new LayerPosition(MapLayerId.Ground, 0, 0),
                    new ActorFaction(1, "Adventurer"),
                    123,
                    ActorBehaviorType.Adventurer,
                    "Alice"),
                10).GetAwaiter().GetResult();

            Assert.That(profileRegistry.TryGetProfile(actorId, out var profile), Is.True);
            Assert.That(profile.DisplayName, Is.EqualTo("Alice"));
            Assert.That(profile.ArchetypeId, Is.EqualTo(1));
        }

        static SpawnAdventurerUseCase CreateSpawnAdventurerUseCase(
            IMasterRepository repository,
            IActorProfileRegistry profileRegistry,
            IEventPublisher eventBus)
        {
            return new SpawnAdventurerUseCase(
                new ActorFactory(repository),
                repository,
                new CompleteActorSpawnUseCase(profileRegistry, eventBus));
        }

        sealed class NoOpActorProfileRegistry : IActorProfileRegistry
        {
            public void Register(Guid actorId, string displayName)
            {
            }
            public void Register(
                Guid actorId,
                string displayName,
                int archetypeId,
                int speciesId,
                ActorBehaviorType behaviorType)
            {
            }
            public bool TryGetProfile(Guid actorId, out ActorProfile profile)
            {
                profile = null;
                return false;
            }
        }

        sealed class RecordingActorProfileRegistry : IActorProfileRegistry
        {
            readonly ActorProfileRegistry inner = new();

            public void Register(Guid actorId, string displayName)
            {
                inner.Register(actorId, displayName);
            }
            public void Register(
                Guid actorId,
                string displayName,
                int archetypeId,
                int speciesId,
                ActorBehaviorType behaviorType)
            {
                inner.Register(actorId, displayName, archetypeId, speciesId, behaviorType);
            }
            public bool TryGetProfile(Guid actorId, out ActorProfile profile)
            {
                return inner.TryGetProfile(actorId, out profile);
            }
        }

        sealed class NoOpGameEventBus : IGameEventBus
        {
            public void Publish(IGameEvent gameEvent)
            {
            }
            public Observable<T> OnEvent<T>() where T : class, IGameEvent
            {
                return Observable.Empty<T>();
            }
        }
    }
}
