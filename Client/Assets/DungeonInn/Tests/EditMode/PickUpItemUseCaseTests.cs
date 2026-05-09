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
using NUnit.Framework;
using R3;

namespace DungeonInn.Tests.EditMode
{
    public sealed class PickUpItemUseCaseTests
    {
        [Test]
        public void ExploringAdventurerPicksUpNearbyItem()
        {
            var (useCase, worldState, eventBus) = CreateContext();
            var actor = CreateAdventurer(
                new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f),
                AdventurerLifecycleState.Exploring);
            var item = new ItemInstance(
                Guid.NewGuid(),
                new ItemStack(1001, 2),
                new LayerPosition(MapLayerId.DungeonFloor(1), 1f, 0f));
            worldState.RegisterActor(actor);
            worldState.AddItem(item);

            useCase.Execute(worldState);

            Assert.That(actor.Inventory.Has(new ItemStack(1001, 2)), Is.True);
            Assert.That(worldState.Items.Count, Is.EqualTo(0));
            var events = eventBus.GetEvents<ItemPickedUp>();
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].ActorId, Is.EqualTo(actor.Id));
            Assert.That(events[0].ItemInstance, Is.EqualTo(item));
        }

        [Test]
        public void NonExploringAdventurerDoesNotPickUpItem()
        {
            var (useCase, worldState, eventBus) = CreateContext();
            var actor = CreateAdventurer(
                new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f),
                AdventurerLifecycleState.Returning);
            var item = new ItemInstance(
                Guid.NewGuid(),
                new ItemStack(1001, 1),
                new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f));
            worldState.RegisterActor(actor);
            worldState.AddItem(item);

            useCase.Execute(worldState);

            Assert.That(actor.Inventory.Has(new ItemStack(1001, 1)), Is.False);
            Assert.That(worldState.Items.Count, Is.EqualTo(1));
            Assert.That(eventBus.GetEvents<ItemPickedUp>().Count, Is.EqualTo(0));
        }

        [Test]
        public void DifferentFloorItemIsNotPickedUp()
        {
            var (useCase, worldState, eventBus) = CreateContext();
            var actor = CreateAdventurer(
                new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f),
                AdventurerLifecycleState.Exploring);
            var item = new ItemInstance(
                Guid.NewGuid(),
                new ItemStack(1001, 1),
                new LayerPosition(MapLayerId.DungeonFloor(2), 0f, 0f));
            worldState.RegisterActor(actor);
            worldState.AddItem(item);

            useCase.Execute(worldState);

            Assert.That(actor.Inventory.Has(new ItemStack(1001, 1)), Is.False);
            Assert.That(worldState.Items.Count, Is.EqualTo(1));
            Assert.That(eventBus.GetEvents<ItemPickedUp>().Count, Is.EqualTo(0));
        }

        [Test]
        public void DistantItemIsNotPickedUp()
        {
            var (useCase, worldState, eventBus) = CreateContext();
            var actor = CreateAdventurer(
                new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f),
                AdventurerLifecycleState.Exploring);
            var item = new ItemInstance(
                Guid.NewGuid(),
                new ItemStack(1001, 1),
                new LayerPosition(MapLayerId.DungeonFloor(1), 10f, 0f));
            worldState.RegisterActor(actor);
            worldState.AddItem(item);

            useCase.Execute(worldState);

            Assert.That(actor.Inventory.Has(new ItemStack(1001, 1)), Is.False);
            Assert.That(worldState.Items.Count, Is.EqualTo(1));
            Assert.That(eventBus.GetEvents<ItemPickedUp>().Count, Is.EqualTo(0));
        }

        [Test]
        public void GoldPickupAddsGoldToInventory()
        {
            var (useCase, worldState, eventBus) = CreateContext();
            var inventory = new Inventory();
            inventory.AddGold(100);
            var actor = CreateAdventurer(
                new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f),
                AdventurerLifecycleState.Exploring,
                inventory);
            var item = new ItemInstance(
                Guid.NewGuid(),
                new ItemStack(SpecialItemIds.Money, 5),
                new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f));
            worldState.RegisterActor(actor);
            worldState.AddItem(item);

            useCase.Execute(worldState);

            Assert.That(actor.Inventory.Gold, Is.EqualTo(105));
            var events = eventBus.GetEvents<ItemPickedUp>();
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].ActorId, Is.EqualTo(actor.Id));
        }

        static (PickUpItemUseCase, GameWorldState, CollectingEventBus) CreateContext()
        {
            var eventBus = new CollectingEventBus();
            return (new PickUpItemUseCase(eventBus), new GameWorldState(), eventBus);
        }

        static Actor CreateAdventurer(LayerPosition position, AdventurerLifecycleState lifecycleState)
        {
            return CreateAdventurer(position, lifecycleState, new Inventory());
        }

        static Actor CreateAdventurer(
            LayerPosition position,
            AdventurerLifecycleState lifecycleState,
            Inventory inventory)
        {
            return new Actor(
                Guid.NewGuid(),
                0,
                new ActorStats(5, 5, 5, 5, 5, 5),
                inventory,
                1,
                0,
                10,
                10,
                0,
                0,
                1,
                position,
                new ActorFaction(1, "Adventurer"),
                new AdventurerBehavior(0, lifecycleState));
        }

        sealed class CollectingEventBus : IGameEventBus
        {
            readonly List<IGameEvent> events = new();

            public void Publish(IGameEvent gameEvent)
            {
                events.Add(gameEvent);
            }

            public Observable<T> OnEvent<T>() where T : class, IGameEvent
            {
                throw new NotSupportedException();
            }

            public IReadOnlyList<T> GetEvents<T>() where T : class, IGameEvent
            {
                return events.OfType<T>().ToList();
            }
        }
    }
}
