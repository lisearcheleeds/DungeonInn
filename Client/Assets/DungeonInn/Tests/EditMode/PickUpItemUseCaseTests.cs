using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.Actors.Ai;
using DungeonInn.Application.Actors.Equipment;
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
            var inventory = new Inventory(new FixedItemStackLimitResolver());
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

        [Test]
        public void FullInventorySkipsNewItemPickup()
        {
            var (useCase, worldState, eventBus) = CreateContext();
            var inventory = CreateFullInventory();
            var actor = CreateAdventurer(
                new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f),
                AdventurerLifecycleState.Exploring,
                inventory);
            var item = new ItemInstance(
                Guid.NewGuid(),
                new ItemStack(2001, 1),
                new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f));
            worldState.RegisterActor(actor);
            worldState.AddItem(item);

            useCase.Execute(worldState);

            Assert.That(actor.Inventory.Has(new ItemStack(2001, 1)), Is.False);
            Assert.That(worldState.Items.Count, Is.EqualTo(1));
            Assert.That(eventBus.GetEvents<ItemPickedUp>().Count, Is.EqualTo(0));
        }

        [Test]
        public void FullInventoryCanPickUpExistingStackItem()
        {
            var (useCase, worldState, eventBus) = CreateContext();
            var inventory = new Inventory(1, new FixedItemStackLimitResolver(10));
            inventory.Add(new ItemStack(1001, 8));
            var actor = CreateAdventurer(
                new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f),
                AdventurerLifecycleState.Exploring,
                inventory);
            var item = new ItemInstance(
                Guid.NewGuid(),
                new ItemStack(1001, 2),
                new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f));
            worldState.RegisterActor(actor);
            worldState.AddItem(item);

            useCase.Execute(worldState);

            Assert.That(actor.Inventory.Has(new ItemStack(1001, 10)), Is.True);
            Assert.That(worldState.Items.Count, Is.EqualTo(0));
            Assert.That(eventBus.GetEvents<ItemPickedUp>().Count, Is.EqualTo(1));
        }

        [Test]
        public void FullInventorySkipsExistingItemWhenStackLimitWouldRequireNewSlot()
        {
            var (useCase, worldState, eventBus) = CreateContext();
            var inventory = new Inventory(1, new FixedItemStackLimitResolver(10));
            inventory.Add(new ItemStack(1001, 10));
            var actor = CreateAdventurer(
                new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f),
                AdventurerLifecycleState.Exploring,
                inventory);
            var item = new ItemInstance(
                Guid.NewGuid(),
                new ItemStack(1001, 1),
                new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f));
            worldState.RegisterActor(actor);
            worldState.AddItem(item);

            useCase.Execute(worldState);

            Assert.That(actor.Inventory.Has(new ItemStack(1001, 11)), Is.False);
            Assert.That(worldState.Items.Count, Is.EqualTo(1));
            Assert.That(eventBus.GetEvents<ItemPickedUp>().Count, Is.EqualTo(0));
        }

        [Test]
        public void ManyActorsAndItemsUseExploringCandidateAndNearbyItemOnly()
        {
            var (useCase, worldState, eventBus) = CreateContext();
            const int actorCount = 120;
            const int itemCount = 120;

            for (var i = 0; i < actorCount; i++)
            {
                worldState.RegisterActor(CreateAdventurer(
                    new LayerPosition(MapLayerId.DungeonFloor(1), i * 3f, 20f),
                    AdventurerLifecycleState.Returning));
            }

            for (var i = 0; i < itemCount; i++)
            {
                worldState.AddItem(new ItemInstance(
                    Guid.NewGuid(),
                    new ItemStack(1001, 1),
                    new LayerPosition(MapLayerId.DungeonFloor(1), 1000f + i * 3f, 0f)));
            }

            var actor = CreateAdventurer(
                new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f),
                AdventurerLifecycleState.Exploring);
            var nearbyItem = new ItemInstance(
                Guid.NewGuid(),
                new ItemStack(1001, 2),
                new LayerPosition(MapLayerId.DungeonFloor(1), 0.5f, 0f));
            worldState.RegisterActor(actor);
            worldState.AddItem(nearbyItem);

            useCase.Execute(worldState);

            Assert.That(actor.Inventory.Has(new ItemStack(1001, 2)), Is.True);
            Assert.That(worldState.Items.Count, Is.EqualTo(itemCount));
            Assert.That(eventBus.GetEvents<ItemPickedUp>().Count, Is.EqualTo(1));
        }

        static (PickUpItemUseCase, GameWorldState, CollectingEventBus) CreateContext()
        {
            var eventBus = new CollectingEventBus();
            var itemSpatialIndexService = new ItemSpatialIndexService(new FixedWorldGameSettingsRepository());
            var candidateService = TestRuntimeServiceFactory.CreateActorProcessingCandidateService();
            return (
                new PickUpItemUseCase(
                    eventBus,
                    itemSpatialIndexService,
                    candidateService,
                    new FixedWorldGameSettingsRepository()),
                new GameWorldState(
                    new ActorSpatialIndexService(new FixedWorldGameSettingsRepository()),
                    itemSpatialIndexService,
                    candidateService,
                    ActorViewDataStoreTestFactory.Create(),
                    new FixedWorldGameSettingsRepository()),
                eventBus);
        }

        static Actor CreateAdventurer(LayerPosition position, AdventurerLifecycleState lifecycleState)
        {
            return CreateAdventurer(position, lifecycleState, new Inventory(new FixedItemStackLimitResolver()));
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
                new AdventurerBehavior(0, lifecycleState),
                WeaponTypeCombatMasterCatalog.Get(WeaponType.Fist));
        }

        static Inventory CreateFullInventory()
        {
            var inventory = new Inventory(new FixedItemStackLimitResolver());
            for (var itemId = 1001; itemId < 1001 + inventory.MaxSlotCount; itemId++)
            {
                inventory.Add(new ItemStack(itemId, 1));
            }

            return inventory;
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


