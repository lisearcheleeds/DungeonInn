using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.UseCase;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using DungeonInn.Master;
using NUnit.Framework;
using R3;

namespace DungeonInn.Tests.EditMode
{
    public sealed class DropItemUseCaseTests
    {
        [Test]
        public void EmptyDropTableProducesNoItems()
        {
            var (useCase, worldState, eventBus, _) = CreateContext(fixedRoll: 0);
            var actor = CreateMonsterActor(new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f), Array.Empty<ActorDropEntry>());

            useCase.Execute(actor, worldState);

            Assert.That(eventBus.GetEvents<ItemDropped>().Count, Is.EqualTo(0));
            Assert.That(worldState.Items.Count, Is.EqualTo(0));
        }

        [Test]
        public void ProbabilityPassDropsItemAndAddsToWorldState()
        {
            var (useCase, worldState, eventBus, _) = CreateContext(fixedRoll: 0);
            var drops = new[] { new ActorDropEntry(1001, 1.0f, 1, 1) };
            var actor = CreateMonsterActor(new LayerPosition(MapLayerId.DungeonFloor(1), 3f, 7f), drops);

            useCase.Execute(actor, worldState);

            var droppedEvents = eventBus.GetEvents<ItemDropped>();
            Assert.That(droppedEvents.Count, Is.EqualTo(1));
            Assert.That(droppedEvents[0].ActorId, Is.EqualTo(actor.Id));
            Assert.That(droppedEvents[0].ItemId, Is.EqualTo(1001));
            Assert.That(droppedEvents[0].ItemName, Is.EqualTo("Herb"));
            Assert.That(worldState.Items.Count, Is.EqualTo(1));
            Assert.That(worldState.Items[0].ItemId, Is.EqualTo(1001));
        }

        [Test]
        public void ProbabilityFailProducesNoItems()
        {
            // roll = 5001/10000 = 0.5001 which is > 0.5 → skip
            var (useCase, worldState, eventBus, _) = CreateContext(fixedRoll: 5001);
            var drops = new[] { new ActorDropEntry(1001, 0.5f, 1, 1) };
            var actor = CreateMonsterActor(new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f), drops);

            useCase.Execute(actor, worldState);

            Assert.That(eventBus.GetEvents<ItemDropped>().Count, Is.EqualTo(0));
            Assert.That(worldState.Items.Count, Is.EqualTo(0));
        }

        [Test]
        public void CountRangeDropsMultipleInstances()
        {
            var (useCase, worldState, eventBus, _) = CreateContext(fixedRoll: 0);
            var drops = new[] { new ActorDropEntry(1001, 1.0f, 3, 3) };
            var actor = CreateMonsterActor(new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f), drops);

            useCase.Execute(actor, worldState);

            Assert.That(eventBus.GetEvents<ItemDropped>().Count, Is.EqualTo(3));
            Assert.That(worldState.Items.Count, Is.EqualTo(3));
        }

        [Test]
        public void MultipleEntriesEachRolledIndependently()
        {
            var (useCase, worldState, eventBus, _) = CreateContext(fixedRoll: 0);
            var drops = new[]
            {
                new ActorDropEntry(1001, 1.0f, 1, 1),
                new ActorDropEntry(1002, 1.0f, 1, 1)
            };
            var actor = CreateMonsterActor(new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f), drops);

            useCase.Execute(actor, worldState);

            var droppedEvents = eventBus.GetEvents<ItemDropped>();
            Assert.That(droppedEvents.Count, Is.EqualTo(2));
            Assert.That(droppedEvents.Any(e => e.ItemId == 1001), Is.True);
            Assert.That(droppedEvents.Any(e => e.ItemId == 1002), Is.True);
        }

        [Test]
        public void ActorWithAdventurerBehaviorProducesNoItems()
        {
            var (useCase, worldState, eventBus, _) = CreateContext(fixedRoll: 0);
            var actor = CreateAdventurerActor(new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f));

            useCase.Execute(actor, worldState);

            Assert.That(eventBus.GetEvents<ItemDropped>().Count, Is.EqualTo(0));
        }

        (DropItemUseCase, GameWorldState, CollectingEventBus, FixedGameRandom) CreateContext(int fixedRoll)
        {
            var eventBus = new CollectingEventBus();
            var random = new FixedGameRandom(fixedRoll);
            var useCase = new DropItemUseCase(new StubItemMasterRepository(), random, eventBus);
            var worldState = new GameWorldState();
            return (useCase, worldState, eventBus, random);
        }

        static Actor CreateMonsterActor(LayerPosition position, IReadOnlyList<ActorDropEntry> dropTable)
        {
            return new Actor(
                Guid.NewGuid(),
                0,
                new ActorStats(5, 5, 5, 5, 5, 5),
                new Inventory(),
                1,
                0,
                10,
                10,
                0,
                0,
                1,
                position,
                new ActorFaction(2, "Monster"),
                new MonsterBehavior(1, false, dropTable));
        }

        static Actor CreateAdventurerActor(LayerPosition position)
        {
            return new Actor(
                Guid.NewGuid(),
                0,
                new ActorStats(5, 5, 5, 5, 5, 5),
                new Inventory(),
                1,
                0,
                10,
                10,
                0,
                0,
                1,
                position,
                new ActorFaction(1, "Adventurer"),
                new AdventurerBehavior(0));
        }

        sealed class StubItemMasterRepository : IMasterRepository
        {
            static readonly Dictionary<int, ItemMaster> masters = new()
            {
                { 1001, new ItemMaster(1001, "Herb", ItemCategory.Material, 10, 1, true) },
                { 1002, new ItemMaster(1002, "Goblin Ear", ItemCategory.Material, 25, 1, true) }
            };

            public IReadOnlyDictionary<int, ItemMaster> ItemMasters => masters;
            public IReadOnlyDictionary<int, EquipmentMaster> EquipmentMasters => throw new NotSupportedException();
            public IReadOnlyDictionary<int, WeaponMaster> WeaponMasters => throw new NotSupportedException();
            public IReadOnlyDictionary<WeaponType, WeaponTypeCombatMaster> WeaponTypeCombatMasters => throw new NotSupportedException();
            public IReadOnlyDictionary<int, ActorArchetypeMaster> ActorArchetypeMasters => throw new NotSupportedException();
            public IReadOnlyDictionary<int, MonsterSpeciesMaster> MonsterSpeciesMasters => throw new NotSupportedException();
            public IReadOnlyDictionary<int, SpawnTableMaster> SpawnTableMasters => throw new NotSupportedException();
            public IReadOnlyDictionary<int, LevelTable> LevelTables => throw new NotSupportedException();
            public ItemMaster GetItemMaster(int itemId) => masters[itemId];
            public EquipmentMaster GetEquipmentMaster(int itemId) => throw new NotSupportedException();
            public WeaponMaster GetWeaponMaster(int itemId) => throw new NotSupportedException();
            public WeaponTypeCombatMaster GetWeaponTypeCombatMaster(WeaponType weaponType) => throw new NotSupportedException();
            public ActorArchetypeMaster GetActorArchetypeMaster(int archetypeId) => throw new NotSupportedException();
            public MonsterSpeciesMaster GetMonsterSpeciesMaster(int speciesId) => throw new NotSupportedException();
            public SpawnTableMaster GetSpawnTableMaster(int spawnTableId) => throw new NotSupportedException();
            public LevelTable GetLevelTable(int levelTableId) => throw new NotSupportedException();
        }

        sealed class FixedGameRandom : IGameRandom
        {
            readonly int value;
            public FixedGameRandom(int value) => this.value = value;
            public int Next() => value;
            public int Next(int maxExclusive) => Math.Min(value, maxExclusive - 1);
            public int Next(int minInclusive, int maxExclusive) => Math.Clamp(value, minInclusive, maxExclusive - 1);
        }

        sealed class CollectingEventBus : IGameEventBus
        {
            readonly List<IGameEvent> events = new();
            public void Publish(IGameEvent gameEvent) => events.Add(gameEvent);
            public Observable<T> OnEvent<T>() where T : class, IGameEvent => throw new NotSupportedException();
            public IReadOnlyList<T> GetEvents<T>() where T : class, IGameEvent => events.OfType<T>().ToList();
        }
    }
}
