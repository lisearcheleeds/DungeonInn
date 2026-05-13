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
    public sealed class DropItemServiceTests
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
            Assert.That(droppedEvents[0].ItemInstance.Stack.ItemId, Is.EqualTo(1001));
            Assert.That(droppedEvents[0].ItemInstance.Stack.Count, Is.EqualTo(1));
            Assert.That(worldState.Items.Count, Is.EqualTo(1));
            Assert.That(worldState.Items[0].Stack.ItemId, Is.EqualTo(1001));
            Assert.That(worldState.Items[0].Stack.Count, Is.EqualTo(1));
        }

        [Test]
        public void ProbabilityFailProducesNoItems()
        {
            // roll = 5001/10000 = 0.5001 which is > 0.5, so skip
            var (useCase, worldState, eventBus, _) = CreateContext(fixedRoll: 5001);
            var drops = new[] { new ActorDropEntry(1001, 0.5f, 1, 1) };
            var actor = CreateMonsterActor(new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f), drops);

            useCase.Execute(actor, worldState);

            Assert.That(eventBus.GetEvents<ItemDropped>().Count, Is.EqualTo(0));
            Assert.That(worldState.Items.Count, Is.EqualTo(0));
        }

        [Test]
        public void FixedCountDropsOneStackWithCorrectAmount()
        {
            var (useCase, worldState, eventBus, _) = CreateContext(fixedRoll: 0);
            var drops = new[] { new ActorDropEntry(1001, 1.0f, 3, 3) };
            var actor = CreateMonsterActor(new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f), drops);

            useCase.Execute(actor, worldState);

            Assert.That(eventBus.GetEvents<ItemDropped>().Count, Is.EqualTo(1));
            Assert.That(worldState.Items.Count, Is.EqualTo(1));
            Assert.That(worldState.Items[0].Stack.Count, Is.EqualTo(3));
        }

        [Test]
        public void RangeCountUsesRandomAmountWithinBounds()
        {
            // FixedGameRandom.Next(min, max) returns min, so amount = MinCount
            var (useCase, worldState, _, _) = CreateContext(fixedRoll: 0);
            var drops = new[] { new ActorDropEntry(1001, 1.0f, 1, 3) };
            var actor = CreateMonsterActor(new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f), drops);

            useCase.Execute(actor, worldState);

            Assert.That(worldState.Items[0].Stack.Count, Is.InRange(1, 3));
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
            Assert.That(droppedEvents.Any(droppedEvent => droppedEvent.ItemInstance.Stack.ItemId == 1001), Is.True);
            Assert.That(droppedEvents.Any(droppedEvent => droppedEvent.ItemInstance.Stack.ItemId == 1002), Is.True);
        }

        [Test]
        public void ActorWithAdventurerBehaviorProducesNoItems()
        {
            var (useCase, worldState, eventBus, _) = CreateContext(fixedRoll: 0);
            var actor = CreateAdventurerActor(new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f));

            useCase.Execute(actor, worldState);

            Assert.That(eventBus.GetEvents<ItemDropped>().Count, Is.EqualTo(0));
        }

        (DropItemService, GameWorldState, CollectingEventBus, FixedGameRandom) CreateContext(int fixedRoll)
        {
            var eventBus = new CollectingEventBus();
            var random = new FixedGameRandom(fixedRoll);
            var service = new DropItemService(random, eventBus);
            var worldState = new GameWorldState();
            return (service, worldState, eventBus, random);
        }

        static Actor CreateMonsterActor(LayerPosition position, IReadOnlyList<ActorDropEntry> dropTable)
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
                new ActorFaction(2, "Monster"),
                new MonsterBehavior(1, dropTable),
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

