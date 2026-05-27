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
        static readonly InitialWorldSettings InitialWorld = InitialWorldSettings.CreateDefault();
        static readonly InnBalanceSettings InnBalance = InnBalanceSettings.CreateDefault();
        static ActorProcessingCandidateService currentCandidateService;

        [Test]
        public void RecoveringAdventurerWaitsWhenInnIsFull()
        {
            var worldState = CreateInitializedWorldState();
            var inn = worldState.Guild.Facilities[0];
            FillInn(worldState, inn.Capacity);
            var eventBus = new CollectingEventBus();
            var actor = CreateAdventurer(AdventurerLifecycleState.Recovering, InnBalance.FeePerStay);
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
            var actor = CreateAdventurer(AdventurerLifecycleState.Recovering, InnBalance.FeePerStay);
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
            var actor = CreateAdventurer(AdventurerLifecycleState.Recovering, InnBalance.FeePerStay);
            worldState.RegisterActor(actor);
            var useCase = CreateUseCase(eventBus, clock);
            useCase.EnsureReservationsAsync(worldState, 1).GetAwaiter().GetResult();

            clock.CurrentDayValue = InnBalance.AdventurerWaitDepartureDays;
            useCase.EnsureReservationsAsync(worldState, 2).GetAwaiter().GetResult();

            Assert.That(worldState.FindActor(actor.Id), Is.Null);
            var events = eventBus.GetEvents<ActorDeparted>();
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].ActorId, Is.EqualTo(actor.Id));
            Assert.That(events[0].WaitedDays, Is.EqualTo(InnBalance.AdventurerWaitDepartureDays));
        }

        [Test]
        public void ReservedAdventurerDoesNotDepartAfterThreeGameDays()
        {
            var worldState = CreateInitializedWorldState();
            var inn = worldState.Guild.Facilities[0];
            var eventBus = new CollectingEventBus();
            var clock = new StubGameClock { CurrentDayValue = InnBalance.AdventurerWaitDepartureDays };
            var actor = CreateAdventurer(AdventurerLifecycleState.WaitingForInn, InnBalance.FeePerStay);
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
            var actor = CreateAdventurer(AdventurerLifecycleState.WaitingForInn, InnBalance.FeePerStay);
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
            actor.GainItem(new ItemStack(1001, 2));
            worldState.RegisterActor(actor);
            var eventBus = new CollectingEventBus();
            var useCase = new SellItemsUseCase(new HardcodedMasterRepository(), eventBus, new StubGameClock(), currentCandidateService);

            useCase.Execute(worldState);

            Assert.That(actor.Inventory.Gold, Is.EqualTo(10));
            Assert.That(eventBus.GetEvents<ItemSold>().Count, Is.EqualTo(1));
        }

        static AdvanceInnRecoveryOrchestrator CreateUseCase(IGameEventBus eventBus)
        {
            return CreateUseCase(eventBus, new StubGameClock());
        }

        static AdvanceInnRecoveryOrchestrator CreateUseCase(IGameEventBus eventBus, IGameClock gameClock)
        {
            return new AdvanceInnRecoveryOrchestrator(
                new RecoverAdventurerAtInnUseCase(
                    eventBus,
                    gameClock,
                    new AdventurerRecoveryStateService(eventBus),
                    currentCandidateService,
                    new FixedWorldGameSettingsRepository(),
                    new FacilityEffectService()),
                new ChargeInnFeeUseCase(eventBus, new FixedWorldGameSettingsRepository()),
                new DespawnAdventurerUseCase(eventBus),
                eventBus,
                gameClock,
                currentCandidateService,
                new FixedWorldGameSettingsRepository());
        }

        static GameWorldState CreateInitializedWorldState()
        {
            currentCandidateService = TestRuntimeServiceFactory.CreateActorProcessingCandidateService();
            var worldState = new GameWorldState(
                new ActorSpatialIndexService(new FixedWorldGameSettingsRepository()),
                new ItemSpatialIndexService(new FixedWorldGameSettingsRepository()),
                currentCandidateService,
                ActorViewDataStoreTestFactory.Create(),
                new FixedWorldGameSettingsRepository());
            var useCase = new InitializeGameWorldOrchestrator(
                worldState,
                new InitializeWorldMapUseCase(new FixedWorldGameSettingsRepository()),
                new InitializeDungeonOrchestrator(new GenerateDungeonFloorUseCase(new FixedWorldGameSettingsRepository())),
                new HardcodedMasterRepository(),
                new CollectingEventBus(),
                new FixedWorldGameSettingsRepository());

            useCase.ExecuteAsync(
                    new InitializeGameWorldRequest(
                        InitialWorld.DungeonSeed,
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
                var actor = CreateAdventurer(AdventurerLifecycleState.Recovering, InnBalance.FeePerStay);
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
                new AdventurerBehavior(0, lifecycleState),
                WeaponTypeCombatMasterCatalog.Get(WeaponType.Fist));
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
            int totalScheduleTickValue;

            public int CurrentDayValue
            {
                get => CurrentDay;
                set => totalScheduleTickValue = GameTimeUtility.GetDayStartTick(value);
            }

            public int CurrentScheduleTickValue
            {
                get => totalScheduleTickValue;
                set => totalScheduleTickValue = value;
            }

            public int TotalScheduleTick => totalScheduleTickValue;
            public int CurrentScheduleTick => totalScheduleTickValue;
            public int CurrentDay => GameTimeUtility.GetDay(totalScheduleTickValue);
            public int CurrentTickOfDay => GameTimeUtility.GetTickOfDay(totalScheduleTickValue);
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
                return new GameClockAdvanceResult(0, Array.Empty<int>());
            }
        }
    }
}

