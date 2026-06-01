using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Application.Actors.Ai;
using DungeonInn.Application.Actors.Lifecycle;
using DungeonInn.Application.Actors.Movement;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Dungeons;
using DungeonInn.Application.Economy;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.Facilities;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.Items;
using DungeonInn.Application.World;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Facility;
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
        static FacilityBuildingRegistry currentFacilityBuildingRegistry;
        static ActorSpatialIndexService currentActorSpatialIndexService;
        static ActorFacilityPresenceService currentActorFacilityPresenceService;
        static ActorViewDataStore currentActorViewDataStore;

        [Test]
        public void InnInteractionWaitsOutsideWhenInnIsFull()
        {
            var worldState = CreateInitializedWorldState();
            var inn = worldState.Guild.Facilities[0];
            FillInn(worldState, inn.Capacity);
            var eventBus = new CollectingEventBus();
            var actor = CreateAdventurer(AdventurerLifecycleState.Recovering, InnBalance.FeePerStay);
            worldState.RegisterActor(actor);
            var orchestrator = CreateFacilityInteractionOrchestrator(eventBus, new StubGameClock());

            var result = orchestrator.ExecuteAsync(worldState, actor, inn).GetAwaiter().GetResult();

            Assert.That(result, Is.EqualTo(FacilityInteractionResult.WaitingOutside));
            Assert.That(actor.RequireBehavior<AdventurerBehavior>().LifecycleState, Is.EqualTo(AdventurerLifecycleState.WaitingForInn));
            Assert.That(worldState.Guild.CountQueuedInnReservations(inn.Id), Is.EqualTo(1));
            Assert.That(worldState.Guild.TryPeekQueuedInnReservation(inn.Id, out var queuedActorId), Is.True);
            Assert.That(queuedActorId, Is.EqualTo(actor.Id));
            Assert.That(eventBus.GetEvents<ActorWaitingForInn>().Count, Is.EqualTo(1));
            Assert.That(eventBus.GetEvents<ActorAiDecisionRecorded>()[0].DecisionType, Is.EqualTo(AiDecisionType.WaitForInn));
        }

        [Test]
        public void QueuedAdventurerEntersInnWhenRoomBecomesAvailable()
        {
            var worldState = CreateInitializedWorldState();
            var inn = worldState.Guild.Facilities[0];
            var filler = FillInn(worldState, inn.Capacity)[0];
            var eventBus = new CollectingEventBus();
            var clock = new StubGameClock();
            var actor = CreateAdventurer(AdventurerLifecycleState.Recovering, InnBalance.FeePerStay);
            worldState.RegisterActor(actor);
            CreateFacilityInteractionOrchestrator(eventBus, clock)
                .ExecuteAsync(worldState, actor, inn)
                .GetAwaiter()
                .GetResult();
            worldState.Guild.ReleaseInnReservation(filler.Id, 2);
            MoveToFacilityInteractionPoint(actor, inn.Id);

            CreateGroundFacilityTask(eventBus, clock)
                .ExecuteAsync(worldState, actor, actor.RequireBehavior<AdventurerBehavior>(), 0f)
                .GetAwaiter()
                .GetResult();

            Assert.That(worldState.Guild.HasActiveInnReservation(actor.Id), Is.True);
            Assert.That(actor.RequireBehavior<AdventurerBehavior>().LifecycleState, Is.EqualTo(AdventurerLifecycleState.Recovering));
            Assert.That(actor.Inventory.Gold, Is.EqualTo(0));
            Assert.That(eventBus.GetEvents<ActorReservedInn>().Count, Is.EqualTo(1));
        }

        [Test]
        public void QueuedAdventurersReserveInnByQueueOrder()
        {
            var worldState = CreateInitializedWorldState();
            var inn = worldState.Guild.Facilities[0];
            var filler = FillInn(worldState, inn.Capacity)[0];
            var eventBus = new CollectingEventBus();
            var clock = new StubGameClock();
            var firstActor = CreateAdventurer(AdventurerLifecycleState.Recovering, InnBalance.FeePerStay);
            var secondActor = CreateAdventurer(AdventurerLifecycleState.Recovering, InnBalance.FeePerStay);
            worldState.RegisterActor(firstActor);
            worldState.RegisterActor(secondActor);
            var interaction = CreateFacilityInteractionOrchestrator(eventBus, clock);
            interaction.ExecuteAsync(worldState, firstActor, inn).GetAwaiter().GetResult();
            interaction.ExecuteAsync(worldState, secondActor, inn).GetAwaiter().GetResult();
            worldState.Guild.ReleaseInnReservation(filler.Id, 2);
            MoveToFacilityInteractionPoint(firstActor, inn.Id);
            MoveToFacilityInteractionPoint(secondActor, inn.Id);
            var task = CreateGroundFacilityTask(eventBus, clock);

            task.ExecuteAsync(worldState, secondActor, secondActor.RequireBehavior<AdventurerBehavior>(), 0f)
                .GetAwaiter()
                .GetResult();
            task.ExecuteAsync(worldState, firstActor, firstActor.RequireBehavior<AdventurerBehavior>(), 0f)
                .GetAwaiter()
                .GetResult();

            Assert.That(worldState.Guild.HasActiveInnReservation(firstActor.Id), Is.True);
            Assert.That(worldState.Guild.HasActiveInnReservation(secondActor.Id), Is.False);
            Assert.That(worldState.Guild.TryPeekQueuedInnReservation(inn.Id, out var queuedActorId), Is.True);
            Assert.That(queuedActorId, Is.EqualTo(secondActor.Id));
        }

        [Test]
        public void InnInteractionReservesWithPartialFeeWhenInnFeeCannotBePaid()
        {
            var worldState = CreateInitializedWorldState();
            var inn = worldState.Guild.Facilities[0];
            var eventBus = new CollectingEventBus();
            var actor = CreateAdventurer(AdventurerLifecycleState.Recovering, 0);
            worldState.RegisterActor(actor);
            var orchestrator = CreateFacilityInteractionOrchestrator(eventBus, new StubGameClock());

            var result = orchestrator.ExecuteAsync(worldState, actor, inn).GetAwaiter().GetResult();

            Assert.That(result, Is.EqualTo(FacilityInteractionResult.StayingInside));
            Assert.That(actor.RequireBehavior<AdventurerBehavior>().LifecycleState, Is.EqualTo(AdventurerLifecycleState.Recovering));
            Assert.That(actor.RequireBehavior<AdventurerBehavior>().WaitingForInnStartedDay, Is.EqualTo(-1));
            Assert.That(worldState.Guild.HasActiveInnReservation(actor.Id), Is.True);
            Assert.That(eventBus.GetEvents<ActorWaitingForInn>().Count, Is.EqualTo(0));
            Assert.That(eventBus.GetEvents<InnFeeCharged>()[0].FeeAmount, Is.EqualTo(0));
        }

        [Test]
        public void ActiveInnGuestRecoversEvenWhenRecoveryCandidateIsMissing()
        {
            var worldState = CreateInitializedWorldState();
            var inn = worldState.Guild.Facilities[0];
            var eventBus = new CollectingEventBus();
            var actor = CreateAdventurer(AdventurerLifecycleState.Recovering, InnBalance.FeePerStay);
            actor.ReceiveDamage(5);
            worldState.RegisterActor(actor);
            worldState.Guild.ReserveInn(Guid.NewGuid(), actor, inn.Id, 1);
            currentCandidateService.ClearRecoveryCandidate(actor.Id);
            var hpBefore = actor.Hp;
            var orchestrator = CreateRecoveryProgressOrchestrator(eventBus, new StubGameClock());

            orchestrator.ExecuteAsync(worldState, 600f).GetAwaiter().GetResult();

            Assert.That(actor.Hp, Is.GreaterThan(hpBefore));
            Assert.That(eventBus.GetEvents<ActorRecoveringAtInn>().Count, Is.GreaterThan(0));
        }

        [Test]
        public void ReservationQueueSkipsMissingActorAtHead()
        {
            var worldState = CreateInitializedWorldState();
            var inn = worldState.Guild.Facilities[0];
            var eventBus = new CollectingEventBus();
            var clock = new StubGameClock();
            var missingActor = CreateAdventurer(AdventurerLifecycleState.WaitingForInn, InnBalance.FeePerStay);
            missingActor.RequireBehavior<AdventurerBehavior>().StartWaitingForInn(clock.CurrentDay);
            var actor = CreateAdventurer(AdventurerLifecycleState.WaitingForInn, InnBalance.FeePerStay);
            actor.RequireBehavior<AdventurerBehavior>().StartWaitingForInn(clock.CurrentDay);
            worldState.Guild.EnqueueInnReservation(missingActor, inn.Id);
            worldState.RegisterActor(actor);
            worldState.Guild.EnqueueInnReservation(actor, inn.Id);
            MoveToFacilityInteractionPoint(actor, inn.Id);

            CreateGroundFacilityTask(eventBus, clock)
                .ExecuteAsync(worldState, actor, actor.RequireBehavior<AdventurerBehavior>(), 0f)
                .GetAwaiter()
                .GetResult();

            Assert.That(worldState.Guild.HasActiveInnReservation(actor.Id), Is.True);
            Assert.That(worldState.Guild.CountQueuedInnReservations(inn.Id), Is.EqualTo(0));
        }

        [Test]
        public void WaitingAdventurerDepartsAfterThreeGameDaysWithoutInn()
        {
            var worldState = CreateInitializedWorldState();
            var inn = worldState.Guild.Facilities[0];
            var eventBus = new CollectingEventBus();
            var clock = new StubGameClock();
            var actor = CreateAdventurer(AdventurerLifecycleState.WaitingForInn, InnBalance.FeePerStay);
            actor.RequireBehavior<AdventurerBehavior>().StartWaitingForInn(0);
            worldState.RegisterActor(actor);
            worldState.Guild.EnqueueInnReservation(actor, inn.Id);
            clock.CurrentDayValue = InnBalance.AdventurerWaitDepartureDays;

            CreateGroundFacilityTask(eventBus, clock)
                .ExecuteAsync(worldState, actor, actor.RequireBehavior<AdventurerBehavior>(), 0f)
                .GetAwaiter()
                .GetResult();

            Assert.That(worldState.FindActor(actor.Id), Is.Null);
            Assert.That(eventBus.GetEvents<ActorDeparted>().Count, Is.EqualTo(1));
            Assert.That(eventBus.GetEvents<ActorDeparted>()[0].WaitedDays, Is.EqualTo(InnBalance.AdventurerWaitDepartureDays));
        }

        [Test]
        public void ActiveReservationDoesNotDepartAfterThreeGameDays()
        {
            var worldState = CreateInitializedWorldState();
            var inn = worldState.Guild.Facilities[0];
            var eventBus = new CollectingEventBus();
            var clock = new StubGameClock { CurrentDayValue = InnBalance.AdventurerWaitDepartureDays };
            var actor = CreateAdventurer(AdventurerLifecycleState.Recovering, InnBalance.FeePerStay);
            actor.RequireBehavior<AdventurerBehavior>().StartWaitingForInn(0);
            worldState.RegisterActor(actor);
            worldState.Guild.ReserveInn(Guid.NewGuid(), actor, inn.Id, 1);
            var orchestrator = CreateRecoveryProgressOrchestrator(eventBus, clock);

            orchestrator.ExecuteAsync(worldState, 60f).GetAwaiter().GetResult();

            Assert.That(worldState.FindActor(actor.Id), Is.EqualTo(actor));
            Assert.That(worldState.Guild.HasActiveInnReservation(actor.Id), Is.True);
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
            var orchestrator = CreateRecoveryProgressOrchestrator(eventBus, new StubGameClock());

            orchestrator.ExecuteAsync(worldState, 60f).GetAwaiter().GetResult();

            Assert.That(actor.Hp, Is.EqualTo(hpBefore));
            Assert.That(eventBus.GetEvents<ActorRecoveringAtInn>().Count, Is.EqualTo(0));
        }

        [Test]
        public void WaitingAdventurerCanUseGeneralStoreBeforeInnFeePayment()
        {
            var worldState = CreateInitializedWorldState();
            var generalStore = worldState.Guild.Facilities.First(x => x.Type == FacilityType.GeneralStore);
            var actor = CreateAdventurer(AdventurerLifecycleState.WaitingForInn, 0);
            actor.GainItem(new ItemStack(1002, 2));
            worldState.RegisterActor(actor);
            var eventBus = new CollectingEventBus();
            var orchestrator = CreateFacilityInteractionOrchestrator(eventBus, new StubGameClock());

            orchestrator.ExecuteAsync(worldState, actor, generalStore).GetAwaiter().GetResult();

            Assert.That(actor.Inventory.Gold, Is.EqualTo(25));
            Assert.That(eventBus.GetEvents<ItemSold>().Count, Is.EqualTo(1));
        }

        [Test]
        public void RecoveringAdventurerIgnoresStalePreparePlanUntilFullyRecovered()
        {
            var worldState = CreateInitializedWorldState();
            var actor = CreateAdventurer(AdventurerLifecycleState.Recovering, InnBalance.FeePerStay);
            actor.ReceiveDamage(1);
            actor.ChangePlan(new ActorPlan(ActorPlanType.Prepare, 0, 0));
            worldState.RegisterActor(actor);
            var eventBus = new CollectingEventBus();

            CreateGroundFacilityTask(eventBus, new StubGameClock())
                .ExecuteAsync(worldState, actor, actor.RequireBehavior<AdventurerBehavior>(), 0f)
                .GetAwaiter()
                .GetResult();

            Assert.That(actor.RequireBehavior<AdventurerBehavior>().LifecycleState, Is.EqualTo(AdventurerLifecycleState.Recovering));
            Assert.That(actor.CurrentPlan.Type, Is.EqualTo(ActorPlanType.None));
            Assert.That(actor.CurrentAction.Type, Is.EqualTo(ActorActionType.None));
        }

        static AdvanceInnRecoveryOrchestrator CreateRecoveryProgressOrchestrator(
            IGameEventBus eventBus,
            IGameClock gameClock)
        {
            return new AdvanceInnRecoveryOrchestrator(new RecoverAdventurerAtInnUseCase(
                eventBus,
                gameClock,
                new AdventurerRecoveryStateService(eventBus),
                currentCandidateService,
                new FixedWorldGameSettingsRepository(),
                new FacilityEffectService(),
                currentActorFacilityPresenceService,
                currentActorViewDataStore));
        }

        static FacilityInteractionOrchestrator CreateFacilityInteractionOrchestrator(
            IGameEventBus eventBus,
            IGameClock gameClock)
        {
            var masterRepository = new HardcodedMasterRepository();
            var settingsRepository = new FixedWorldGameSettingsRepository();
            var lineupUseCase = new GetFacilityLineupUseCase(masterRepository, masterRepository);
            return new FacilityInteractionOrchestrator(
                new ChargeInnFeeUseCase(eventBus, settingsRepository),
                new FacilityNeedSelector(masterRepository, lineupUseCase),
                lineupUseCase,
                masterRepository,
                gameClock,
                eventBus,
                currentCandidateService,
                settingsRepository);
        }

        static AdvanceGroundFacilityTaskOrchestrator CreateGroundFacilityTask(
            IGameEventBus eventBus,
            IGameClock gameClock)
        {
            var navigationService = new ActorNavigationService(eventBus, new NoOpNavigationPathProvider());
            return new AdvanceGroundFacilityTaskOrchestrator(
                new MoveActorTowardDestinationUseCase(new ActorMovementService(
                    navigationService,
                    currentActorSpatialIndexService,
                    currentActorViewDataStore)),
                currentFacilityBuildingRegistry,
                CreateFacilityInteractionOrchestrator(eventBus, gameClock),
                currentActorFacilityPresenceService,
                currentActorSpatialIndexService,
                currentActorViewDataStore,
                new FixedWorldGameSettingsRepository(),
                TestRuntimeServiceFactory.CreateActorDecisionScheduler(),
                gameClock,
                new DespawnAdventurerUseCase(eventBus));
        }

        static GameWorldState CreateInitializedWorldState()
        {
            var settingsRepository = new FixedWorldGameSettingsRepository();
            currentCandidateService = TestRuntimeServiceFactory.CreateActorProcessingCandidateService();
            currentFacilityBuildingRegistry = new FacilityBuildingRegistry();
            currentActorSpatialIndexService = new ActorSpatialIndexService(settingsRepository);
            currentActorFacilityPresenceService = new ActorFacilityPresenceService();
            currentActorViewDataStore = ActorViewDataStoreTestFactory.Create(currentActorFacilityPresenceService);
            var worldState = new GameWorldState(
                currentActorSpatialIndexService,
                new ItemSpatialIndexService(settingsRepository),
                currentCandidateService,
                currentActorViewDataStore,
                settingsRepository);
            var useCase = new InitializeGameWorldOrchestrator(
                worldState,
                new InitializeWorldMapUseCase(settingsRepository, settingsRepository),
                new InitializeDungeonOrchestrator(new GenerateDungeonFloorUseCase(
                    settingsRepository,
                    new HardcodedMasterRepository(),
                    new AssignDungeonRoomRolesUseCase(new HardcodedMasterRepository()))),
                new HardcodedMasterRepository(),
                new CollectingEventBus(),
                settingsRepository,
                settingsRepository,
                currentFacilityBuildingRegistry);

            useCase.ExecuteAsync(new InitializeGameWorldRequest(InitialWorld.DungeonSeed))
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

        static void MoveToFacilityInteractionPoint(Actor actor, Guid facilityId)
        {
            if (!currentFacilityBuildingRegistry.TryGetByFacilityId(facilityId, out var building))
            {
                throw new InvalidOperationException("Facility building does not exist.");
            }

            actor.MoveTo(building.InteractionPoint.Position);
            currentActorSpatialIndexService.SyncActor(actor);
            currentActorViewDataStore.SyncActor(actor);
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
