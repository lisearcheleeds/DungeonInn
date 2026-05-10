using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.UseCase;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using DungeonInn.Master;
using NUnit.Framework;
using R3;

namespace DungeonInn.Tests.EditMode
{
    public sealed class RecoverAdventurerAtInnUseCaseTests
    {
        [Test]
        public void RecoveringAdventurerWaitsWhenInnIsFull()
        {
            var worldState = CreateInitializedWorldState();
            var inn = worldState.Guild.Facilities[0];
            FillInn(worldState, inn.Capacity);
            var eventBus = new CollectingEventBus();
            var actor = CreateAdventurer(AdventurerLifecycleState.Recovering, GameConstants.InnFeePerStay);
            worldState.RegisterActor(actor);
            var useCase = CreateUseCase(eventBus);

            useCase.EnsureReservationsAsync(worldState, 1).GetAwaiter().GetResult();

            Assert.That(actor.RequireBehavior<AdventurerBehavior>().LifecycleState, Is.EqualTo(AdventurerLifecycleState.WaitingForInn));
            var events = eventBus.GetEvents<ActorWaitingForInn>();
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].ActorId, Is.EqualTo(actor.Id));
            Assert.That(events[0].InnFacilityId, Is.EqualTo(inn.Id));
        }

        [Test]
        public void WaitingAdventurerReservesInnWhenRoomBecomesAvailable()
        {
            var worldState = CreateInitializedWorldState();
            var inn = worldState.Guild.Facilities[0];
            var filler = FillInn(worldState, inn.Capacity)[0];
            var eventBus = new CollectingEventBus();
            var actor = CreateAdventurer(AdventurerLifecycleState.Recovering, GameConstants.InnFeePerStay);
            worldState.RegisterActor(actor);
            var useCase = CreateUseCase(eventBus);
            useCase.EnsureReservationsAsync(worldState, 1).GetAwaiter().GetResult();

            worldState.Guild.ReleaseInnReservation(filler.Id, 2);
            useCase.EnsureReservationsAsync(worldState, 3).GetAwaiter().GetResult();

            Assert.That(actor.RequireBehavior<AdventurerBehavior>().LifecycleState, Is.EqualTo(AdventurerLifecycleState.Recovering));
            Assert.That(worldState.Guild.HasActiveInnReservation(actor.Id), Is.True);
            Assert.That(actor.Inventory.Gold, Is.EqualTo(0));
            Assert.That(eventBus.GetEvents<ActorReservedInn>().Count, Is.EqualTo(1));
        }

        [Test]
        public void WaitingAdventurerDoesNotRecoverWithoutReservation()
        {
            var worldState = CreateInitializedWorldState();
            var eventBus = new CollectingEventBus();
            var actor = CreateAdventurer(AdventurerLifecycleState.WaitingForInn, GameConstants.InnFeePerStay);
            var hpBefore = actor.Hp;
            worldState.RegisterActor(actor);
            var useCase = CreateUseCase(eventBus);

            useCase.ExecuteAsync(worldState, 60f).GetAwaiter().GetResult();

            Assert.That(actor.Hp, Is.EqualTo(hpBefore));
            Assert.That(eventBus.GetEvents<ActorRecoveringAtInn>().Count, Is.EqualTo(0));
        }

        [Test]
        public void WaitingAdventurerCanSellItemsToPayInnFee()
        {
            var worldState = CreateInitializedWorldState();
            var actor = CreateAdventurer(AdventurerLifecycleState.WaitingForInn, 0);
            actor.Inventory.Add(new ItemStack(1001, 1));
            worldState.RegisterActor(actor);
            var eventBus = new CollectingEventBus();
            var useCase = new SellItemsUseCase(new HardcodedMasterRepository(), eventBus);

            useCase.Execute(worldState);

            Assert.That(actor.Inventory.Gold, Is.EqualTo(10));
            Assert.That(eventBus.GetEvents<ItemSold>().Count, Is.EqualTo(1));
        }

        static RecoverAdventurerAtInnUseCase CreateUseCase(IGameEventBus eventBus)
        {
            return new RecoverAdventurerAtInnUseCase(
                eventBus,
                new GameClock(),
                new ChargeInnFeeUseCase(eventBus));
        }

        static GameWorldState CreateInitializedWorldState()
        {
            var worldState = new GameWorldState();
            var useCase = new InitializeGameWorldUseCase(
                worldState,
                new InitializeWorldMapUseCase(),
                new InitializeDungeonUseCase(
                    new EnsureDungeonFloorGeneratedUseCase(
                        new GenerateDungeonFloorUseCase())),
                new HardcodedMasterRepository());

            useCase.ExecuteAsync(
                    new InitializeGameWorldRequest(
                        GameConstants.InitialDungeonSeed,
                        Array.Empty<DungeonDepthBandConfig>()))
                .GetAwaiter()
                .GetResult();

            return worldState;
        }

        static IReadOnlyList<Actor> FillInn(GameWorldState worldState, int count)
        {
            var actors = new List<Actor>();
            var inn = worldState.Guild.Facilities[0];
            for (var i = 0; i < count; i++)
            {
                var actor = CreateAdventurer(AdventurerLifecycleState.Recovering, GameConstants.InnFeePerStay);
                worldState.Guild.ReserveInn(Guid.NewGuid(), actor, inn.Id, i);
                actors.Add(actor);
            }

            return actors;
        }

        static Actor CreateAdventurer(AdventurerLifecycleState lifecycleState, int gold)
        {
            var inventory = new Inventory(new FixedItemStackLimitResolver());
            inventory.AddGold(gold);

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
                new LayerPosition(MapLayerId.Ground, 0f, 0f),
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
                return Observable.Empty<T>();
            }

            public IReadOnlyList<T> GetEvents<T>() where T : class, IGameEvent
            {
                return events.OfType<T>().ToArray();
            }
        }
    }
}
