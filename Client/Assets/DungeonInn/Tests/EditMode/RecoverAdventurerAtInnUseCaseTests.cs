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
            var aiEvents = eventBus.GetEvents<ActorAiDecisionRecorded>();
            Assert.That(aiEvents.Count, Is.EqualTo(1));
            Assert.That(aiEvents[0].DecisionType, Is.EqualTo(AiDecisionType.WaitForInn));
            Assert.That(aiEvents[0].ReasonType, Is.EqualTo(AiDecisionReasonType.NoVacantInnRoom));
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
        public void RecoveringAdventurerReturnsToPreparingWhenInnFeeCannotBePaid()
        {
            var worldState = CreateInitializedWorldState();
            var eventBus = new CollectingEventBus();
            var actor = CreateAdventurer(AdventurerLifecycleState.Recovering, 0);
            worldState.RegisterActor(actor);
            var useCase = CreateUseCase(eventBus);

            useCase.EnsureReservationsAsync(worldState, 1).GetAwaiter().GetResult();

            var behavior = actor.RequireBehavior<AdventurerBehavior>();
            Assert.That(behavior.LifecycleState, Is.EqualTo(AdventurerLifecycleState.Preparing));
            Assert.That(behavior.WaitingForInnStartedDay, Is.EqualTo(-1));
            Assert.That(worldState.Guild.HasActiveInnReservation(actor.Id), Is.False);
            Assert.That(eventBus.GetEvents<ActorWaitingForInn>().Count, Is.EqualTo(0));
        }

        [Test]
        public void WaitingAdventurerDepartsAfterThreeGameDaysWithoutInn()
        {
            var worldState = CreateInitializedWorldState();
            var inn = worldState.Guild.Facilities[0];
            FillInn(worldState, inn.Capacity);
            var eventBus = new CollectingEventBus();
            var clock = new StubGameClock();
            var actor = CreateAdventurer(AdventurerLifecycleState.Recovering, GameConstants.InnFeePerStay);
            worldState.RegisterActor(actor);
            var useCase = CreateUseCase(eventBus, clock);
            useCase.EnsureReservationsAsync(worldState, 1).GetAwaiter().GetResult();

            clock.CurrentDayValue = GameConstants.AdventurerInnWaitDepartureDays;
            useCase.EnsureReservationsAsync(worldState, 2).GetAwaiter().GetResult();

            Assert.That(worldState.FindActor(actor.Id), Is.Null);
            var events = eventBus.GetEvents<ActorDeparted>();
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].ActorId, Is.EqualTo(actor.Id));
            Assert.That(events[0].WaitedDays, Is.EqualTo(GameConstants.AdventurerInnWaitDepartureDays));
        }

        [Test]
        public void ReservedAdventurerDoesNotDepartAfterThreeGameDays()
        {
            var worldState = CreateInitializedWorldState();
            var inn = worldState.Guild.Facilities[0];
            var eventBus = new CollectingEventBus();
            var clock = new StubGameClock { CurrentDayValue = GameConstants.AdventurerInnWaitDepartureDays };
            var actor = CreateAdventurer(AdventurerLifecycleState.WaitingForInn, GameConstants.InnFeePerStay);
            actor.RequireBehavior<AdventurerBehavior>().StartWaitingForInn(0);
            worldState.RegisterActor(actor);
            worldState.Guild.ReserveInn(Guid.NewGuid(), actor, inn.Id, 1);
            var useCase = CreateUseCase(eventBus, clock);

            useCase.EnsureReservationsAsync(worldState, 2).GetAwaiter().GetResult();

            Assert.That(worldState.FindActor(actor.Id), Is.EqualTo(actor));
            Assert.That(actor.RequireBehavior<AdventurerBehavior>().LifecycleState, Is.EqualTo(AdventurerLifecycleState.Recovering));
            Assert.That(eventBus.GetEvents<ActorDeparted>().Count, Is.EqualTo(0));
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
            return CreateUseCase(eventBus, new StubGameClock());
        }

        static RecoverAdventurerAtInnUseCase CreateUseCase(IGameEventBus eventBus, IGameClock gameClock)
        {
            return new RecoverAdventurerAtInnUseCase(
                eventBus,
                gameClock,
                new ChargeInnFeeUseCase(eventBus),
                new DespawnAdventurerUseCase(eventBus));
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

        sealed class StubGameClock : IGameClock
        {
            public int CurrentDayValue { get; set; }
            public int CurrentScheduleTickValue { get; set; }

            public int CurrentScheduleTick => CurrentScheduleTickValue;
            public int CurrentDay => CurrentDayValue;
            public float ElapsedRealTimeSeconds => 0f;
            public float ElapsedGameTimeSeconds => 0f;
            public float TimeScale => 1f;
            public bool IsPaused => false;

            public void SetTimeScale(float timeScale)
            {
            }

            public void Pause()
            {
            }

            public void Resume()
            {
            }

            public GameClockAdvanceResult Advance(float unscaledDeltaTimeSeconds)
            {
                return new GameClockAdvanceResult(0, false);
            }
        }
    }
}
