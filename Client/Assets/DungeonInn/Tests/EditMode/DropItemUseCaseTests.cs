using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.Actors.Ai;
using DungeonInn.Application.Actors.Lifecycle;
using DungeonInn.Application.Actors.Movement;
using DungeonInn.Application.Actors.Profiles;
using DungeonInn.Application.Actors.Spawn;

using DungeonInn.Application.Dungeons;
using DungeonInn.Application.Economy;
using DungeonInn.Application.Facilities;
using DungeonInn.Application.Items;
using DungeonInn.Application.World;
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
        public void SpeciesWithoutDropsProducesNoItems()
        {
            var (useCase, worldState, eventBus, _) = CreateContext(fixedRoll: 0);
            var actor = CreateMonsterActor(1, 10, new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f));

            useCase.Execute(actor, worldState);

            Assert.That(eventBus.GetEvents<ItemDropped>().Count, Is.EqualTo(0));
            Assert.That(worldState.Items.Count, Is.EqualTo(0));
        }

        [Test]
        public void ProbabilityPassDropsItemAndAddsToWorldState()
        {
            var (useCase, worldState, eventBus, _) = CreateContext(fixedRoll: 0);
            var actor = CreateMonsterActor(2, 1, new LayerPosition(MapLayerId.DungeonFloor(1), 3f, 7f));

            useCase.Execute(actor, worldState);

            var droppedEvents = eventBus.GetEvents<ItemDropped>();
            Assert.That(droppedEvents.Count, Is.EqualTo(2));
            Assert.That(droppedEvents[0].ActorId, Is.EqualTo(actor.Id));
            Assert.That(droppedEvents[0].ItemInstance.Stack.ItemId, Is.EqualTo(1002));
            Assert.That(droppedEvents[0].ItemInstance.Stack.Count, Is.EqualTo(1));
            Assert.That(worldState.Items.Count, Is.EqualTo(2));
            Assert.That(worldState.Items[0].Stack.ItemId, Is.EqualTo(1002));
            Assert.That(worldState.Items[0].Stack.Count, Is.EqualTo(1));
        }

        [Test]
        public void ProbabilityFailProducesNoItems()
        {
            var (useCase, worldState, eventBus, _) = CreateContext(fixedRoll: 9000);
            var actor = CreateMonsterActor(2, 1, new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f));

            useCase.Execute(actor, worldState);

            Assert.That(eventBus.GetEvents<ItemDropped>().Count, Is.EqualTo(0));
            Assert.That(worldState.Items.Count, Is.EqualTo(0));
        }

        [Test]
        public void FixedCountDropsOneStackWithCorrectAmount()
        {
            var (useCase, worldState, eventBus, _) = CreateContext(fixedRoll: 0);
            var actor = CreateMonsterActor(2, 1, new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f));

            useCase.Execute(actor, worldState);

            Assert.That(eventBus.GetEvents<ItemDropped>().Count, Is.EqualTo(2));
            Assert.That(worldState.Items.Count, Is.EqualTo(2));
            Assert.That(worldState.Items.Single(item => item.Stack.ItemId == 1002).Stack.Count, Is.EqualTo(1));
        }

        [Test]
        public void RangeCountUsesRandomAmountWithinBounds()
        {
            var (useCase, worldState, _, _) = CreateContext(fixedRoll: 0);
            var actor = CreateMonsterActor(2, 1, new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f));

            useCase.Execute(actor, worldState);

            Assert.That(worldState.Items.Single(item => item.Stack.ItemId == 1).Stack.Count, Is.InRange(1, 4));
        }

        [Test]
        public void MultipleEntriesEachRolledIndependently()
        {
            var (useCase, worldState, eventBus, _) = CreateContext(fixedRoll: 0);
            var actor = CreateMonsterActor(2, 1, new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f));

            useCase.Execute(actor, worldState);

            var droppedEvents = eventBus.GetEvents<ItemDropped>();
            Assert.That(droppedEvents.Count, Is.EqualTo(2));
            Assert.That(droppedEvents.Any(droppedEvent => droppedEvent.ItemInstance.Stack.ItemId == 1002), Is.True);
            Assert.That(droppedEvents.Any(droppedEvent => droppedEvent.ItemInstance.Stack.ItemId == 1), Is.True);
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
            var useCase = new DropItemUseCase(random, eventBus, new HardcodedMasterRepository());
            var worldState = new GameWorldState(
                new ActorSpatialIndexService(new FixedWorldGameSettingsRepository()),
                new ItemSpatialIndexService(new FixedWorldGameSettingsRepository()),
                TestRuntimeServiceFactory.CreateActorProcessingCandidateService(),
                ActorViewDataStoreTestFactory.Create(),
                new FixedWorldGameSettingsRepository());
            return (useCase, worldState, eventBus, random);
        }

        static Actor CreateMonsterActor(int archetypeId, int speciesId, LayerPosition position)
        {
            return new Actor(
                Guid.NewGuid(),
                archetypeId,
                new ActorStats(5, 5, 5, 5, 5, 5),
                new Inventory(new FixedItemStackLimitResolver()),
                1,
                0,
                10,
                10,
                0,
                0,
                1,
                position,
                new ActorFaction(2, "Monster"),
                new MonsterBehavior(speciesId),
                WeaponTypeCombatMasterCatalog.Get(WeaponType.Fist));
        }

        static Actor CreateAdventurerActor(LayerPosition position)
        {
            return new Actor(
                Guid.NewGuid(),
                0,
                new ActorStats(5, 5, 5, 5, 5, 5),
                new Inventory(new FixedItemStackLimitResolver()),
                1,
                0,
                10,
                10,
                0,
                0,
                1,
                position,
                new ActorFaction(1, "Adventurer"),
                new AdventurerBehavior(0),
                WeaponTypeCombatMasterCatalog.Get(WeaponType.Fist));
        }

        sealed class FixedGameRandom : IGameRandom
        {
            readonly int value;
            public FixedGameRandom(int value) => this.value = value;
            public void Initialize(int seed) { }
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




