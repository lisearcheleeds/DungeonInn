using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Actors.Ai;
using DungeonInn.Application.Actors.Equipment;
using DungeonInn.Application.Actors.Lifecycle;
using DungeonInn.Application.Actors.Movement;
using DungeonInn.Application.Actors.Profiles;
using DungeonInn.Application.Actors.Spawn;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Dungeons;
using DungeonInn.Application.Economy;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.Facilities;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.Items;
using DungeonInn.Application.SaveLoad;
using DungeonInn.Application.World;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Combat;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using DungeonInn.Master;
using DungeonInn.GameSession;
using R3;
using UnityEngine;
using DungeonInn.View.Scene.MainScene.World;
using NUnit.Framework;

namespace DungeonInn.Tests.EditMode
{
    public sealed class WorldGameLoopEntryPointArchitectureTests
    {
        static readonly ActorSimulationSettings ActorSimulation = ActorSimulationSettings.CreateDefault();

        [Test]
        public void EntryPointDoesNotDirectlyDependOnApplicationUseCasesOrOrchestrators()
        {
            var entryPointType = typeof(WorldGameLoopEntryPoint);
            var forbiddenDependencies = entryPointType
                .GetFields(System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public)
                .Select(field => field.FieldType)
                .Concat(entryPointType
                    .GetMethods(System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Public)
                    .Where(method => method.Name == "Construct")
                    .SelectMany(method => method.GetParameters())
                    .Select(parameter => parameter.ParameterType))
                .Where(type => type.Namespace != null &&
                    (type.Namespace.EndsWith(".UseCase", System.StringComparison.Ordinal) ||
                    type.Namespace.EndsWith(".Orchestration", System.StringComparison.Ordinal)))
                .Select(type => type.FullName)
                .ToArray();

            Assert.That(forbiddenDependencies, Is.Empty);
        }

        [Test]
        public void NonFrameSimulationSystemsRunOnlyFromScheduleTickBlock()
        {
            var sourcePath = Path.Combine(
                UnityEngine.Application.dataPath,
                "DungeonInn/Runtime/Scripts/Application/World/WorldSimulationOrchestrator.cs");
            var source = File.ReadAllText(sourcePath);
            var frameMethodStart = source.IndexOf("public async UniTask AdvanceFrameAsync", System.StringComparison.Ordinal);
            var scheduleMethodStart = source.IndexOf("async UniTask AdvanceScheduleSystemsAsync", System.StringComparison.Ordinal);

            Assert.That(frameMethodStart, Is.GreaterThanOrEqualTo(0));
            Assert.That(scheduleMethodStart, Is.GreaterThan(frameMethodStart));

            var frameMethodBody = source.Substring(frameMethodStart, scheduleMethodStart - frameMethodStart);
            var scheduleMethodBody = source.Substring(scheduleMethodStart);
            var scheduleOnlyCalls = new[]
            {
                "updateEquipmentUseCase.Execute",
                "sellItemsUseCase.Execute",
                "useRecoveryItemUseCase.ExecuteAsync",
                "decideAdventurerReturnUseCase.ExecuteAsync"
            };

            foreach (var call in scheduleOnlyCalls)
            {
                Assert.That(frameMethodBody, Does.Not.Contain(call));
                Assert.That(scheduleMethodBody, Does.Contain(call));
            }
        }

        [Test]
        public void CombatEncounterDetectionRunsOnlyWhileTimeDependentSystemsAdvance()
        {
            var eventBus = new CollectingEventBus();
            var gameClock = new StubGameClock();
            var actorSpatialIndexService = new ActorSpatialIndexService(new FixedWorldGameSettingsRepository());
            var candidateService = TestRuntimeServiceFactory.CreateActorProcessingCandidateService();
            var actorViewDataStore = ActorViewDataStoreTestFactory.Create();
            var worldState = CreateWorldState(
                actorSpatialIndexService,
                candidateService,
                actorViewDataStore);
            var actor = CreateActor(1, new LayerPosition(MapLayerId.DungeonFloor(1), 5f, 5f));
            var enemy = CreateActor(2, new LayerPosition(MapLayerId.DungeonFloor(1), 7f, 5f));
            worldState.RegisterActor(actor);
            worldState.RegisterActor(enemy);
            var orchestrator = CreateWorldSimulationOrchestrator(
                new TestGameLoopUseCase(new GameLoopTickResult(
                    0,
                    0,
                    Array.Empty<int>(),
                    1f,
                    0f,
                    1f,
                    true)),
                worldState,
                eventBus,
                gameClock,
                actorSpatialIndexService,
                candidateService,
                actorViewDataStore);

            orchestrator.AdvanceFrameAsync(new WorldFrameAdvanceRequest(1f, default))
                .GetAwaiter()
                .GetResult();

            Assert.That(eventBus.GetEvents<CombatEncounterStarted>(), Is.Empty);
            Assert.That(eventBus.GetEvents<ActorAiDecisionRecorded>(), Is.Empty);
        }

        [Test]
        public void ActiveLayerFrameMovementIsNotRepeatedByScheduleMovement()
        {
            var eventBus = new CollectingEventBus();
            var gameClock = new StubGameClock();
            var actorSpatialIndexService = new ActorSpatialIndexService(new FixedWorldGameSettingsRepository());
            var candidateService = TestRuntimeServiceFactory.CreateActorProcessingCandidateService();
            var actorViewDataStore = ActorViewDataStoreTestFactory.Create();
            var worldState = CreateWorldState(
                actorSpatialIndexService,
                candidateService,
                actorViewDataStore);
            var actor = CreateActor(1, new LayerPosition(MapLayerId.Ground, 20.5f, 0.5f));
            actor.RequireBehavior<AdventurerBehavior>()
                .ChangeLifecycleState(AdventurerLifecycleState.GoingToDungeon);
            worldState.RegisterActor(actor);
            var before = actor.Position;
            var orchestrator = CreateWorldSimulationOrchestrator(
                new TestGameLoopUseCase(new GameLoopTickResult(
                    1,
                    1,
                    Array.Empty<int>(),
                    0.5f,
                    1f,
                    1f,
                    false)),
                worldState,
                eventBus,
                gameClock,
                actorSpatialIndexService,
                candidateService,
                actorViewDataStore,
                new DirectNavigationPathProvider());

            orchestrator.AdvanceFrameAsync(
                    new WorldFrameAdvanceRequest(
                        0.5f,
                        default,
                        MapLayerId.Ground))
                .GetAwaiter()
                .GetResult();

            var movedDistance = Math.Sqrt(before.DistanceSquaredTo(actor.Position));
            Assert.That(
                movedDistance,
                Is.EqualTo(ActorSimulation.MoveSpeedMetersPerSecond).Within(0.0001f));
        }

        [Test]
        public void EmptyRealtimeLayerDoesNotReduceLaterScheduledMovement()
        {
            var eventBus = new CollectingEventBus();
            var gameClock = new StubGameClock();
            var actorSpatialIndexService = new ActorSpatialIndexService(new FixedWorldGameSettingsRepository());
            var candidateService = TestRuntimeServiceFactory.CreateActorProcessingCandidateService();
            var actorViewDataStore = ActorViewDataStoreTestFactory.Create();
            var worldState = CreateWorldState(
                actorSpatialIndexService,
                candidateService,
                actorViewDataStore);
            var dungeonActor = CreateActor(1, new LayerPosition(MapLayerId.DungeonFloor(1), 20.5f, 0.5f));
            worldState.RegisterActor(dungeonActor);
            var orchestrator = CreateWorldSimulationOrchestrator(
                new SequenceGameLoopUseCase(
                    new GameLoopTickResult(
                        0,
                        0,
                        Array.Empty<int>(),
                        0.5f,
                        0.5f,
                        1f,
                        false),
                    new GameLoopTickResult(
                        1,
                        1,
                        Array.Empty<int>(),
                        0.5f,
                        1f,
                        1f,
                        false)),
                worldState,
                eventBus,
                gameClock,
                actorSpatialIndexService,
                candidateService,
                actorViewDataStore,
                new DirectNavigationPathProvider());

            orchestrator.AdvanceFrameAsync(
                    new WorldFrameAdvanceRequest(
                        0.5f,
                        default,
                        MapLayerId.Ground))
                .GetAwaiter()
                .GetResult();

            var groundActor = CreateActor(1, new LayerPosition(MapLayerId.Ground, 20.5f, 0.5f));
            groundActor.RequireBehavior<AdventurerBehavior>()
                .ChangeLifecycleState(AdventurerLifecycleState.GoingToDungeon);
            worldState.RegisterActor(groundActor);
            var before = groundActor.Position;

            orchestrator.AdvanceFrameAsync(new WorldFrameAdvanceRequest(0f, default))
                .GetAwaiter()
                .GetResult();

            var movedDistance = Math.Sqrt(before.DistanceSquaredTo(groundActor.Position));
            Assert.That(
                movedDistance,
                Is.EqualTo(ActorSimulation.MoveSpeedMetersPerSecond).Within(0.0001f));
        }

        static WorldSimulationOrchestrator CreateWorldSimulationOrchestrator(
            IGameLoopUseCase gameLoopUseCase,
            GameWorldState worldState,
            CollectingEventBus eventBus,
            IGameClock gameClock,
            ActorSpatialIndexService actorSpatialIndexService,
            ActorProcessingCandidateService candidateService,
            ActorViewDataStore actorViewDataStore)
        {
            return CreateWorldSimulationOrchestrator(
                gameLoopUseCase,
                worldState,
                eventBus,
                gameClock,
                actorSpatialIndexService,
                candidateService,
                actorViewDataStore,
                new NoOpNavigationPathProvider());
        }

        static WorldSimulationOrchestrator CreateWorldSimulationOrchestrator(
            IGameLoopUseCase gameLoopUseCase,
            GameWorldState worldState,
            CollectingEventBus eventBus,
            IGameClock gameClock,
            ActorSpatialIndexService actorSpatialIndexService,
            ActorProcessingCandidateService candidateService,
            ActorViewDataStore actorViewDataStore,
            INavigationPathProvider navigationPathProvider)
        {
            var masterRepository = new HardcodedMasterRepository();
            var itemSpatialIndexService = new ItemSpatialIndexService(new FixedWorldGameSettingsRepository());
            var actorCombatService = new ActorCombatService();
            var navigationService = new ActorNavigationService(
                eventBus,
                navigationPathProvider);
            var profileRegistry = new ActorProfileRegistry();
            var achievementRegistry = new ActorExplorationAchievementRegistry(eventBus);
            var completeActorSpawnUseCase = new CompleteActorSpawnUseCase(profileRegistry, eventBus);
            var actorFactory = new ActorFactory(masterRepository);
            var spawnTableResolver = new SpawnTableResolver(masterRepository);
            var combatDefeatResolver = new CombatDefeatResolver(actorCombatService);
            var actorDefeatOrchestrator = new ActorDefeatOrchestrator(
                combatDefeatResolver,
                new GrantExperienceUseCase(masterRepository, eventBus),
                new DropItemUseCase(new GameRandom(), eventBus, masterRepository));
            var combatEffectExecutor = new CombatEffectExecutor(new CombatDamageResolver(actorCombatService));
            var recoveryStateService = new AdventurerRecoveryStateService(eventBus);

            return new WorldSimulationOrchestrator(
                gameLoopUseCase,
                new GameRandom(),
                worldState,
                new InitializeGameWorldOrchestrator(
                    worldState,
                    new InitializeWorldMapUseCase(new FixedWorldGameSettingsRepository()),
                    new InitializeDungeonOrchestrator(new GenerateDungeonFloorUseCase(new FixedWorldGameSettingsRepository(), new HardcodedMasterRepository(), new AssignDungeonRoomRolesUseCase(new HardcodedMasterRepository()))),
                    masterRepository,
                    eventBus,
                    new FixedWorldGameSettingsRepository()),
                new SpawnScheduledAdventurerOrchestrator(
                    new SpawnAdventurerUseCase(
                        actorFactory,
                        masterRepository,
                        completeActorSpawnUseCase),
                    masterRepository,
                    new GameRandom(),
                    new FixedWorldGameSettingsRepository(),
                    spawnTableResolver,
                    gameClock),
                new SpawnScheduledMonsterOrchestrator(
                    new SpawnMonsterUseCase(
                        actorFactory,
                        masterRepository,
                        completeActorSpawnUseCase),
                    masterRepository,
                    new GameRandom(),
                    new FixedWorldGameSettingsRepository(),
                    spawnTableResolver),
                new AdvanceActorAiOrchestrator(
                    TestRuntimeServiceFactory.CreateActorDecisionScheduler(),
                    new IActorAiPolicy[] { new AdventurerAiPolicy() },
                    new ApplyActorAiDecisionUseCase()),
                new AdvanceActorLifecycleOrchestrator(
                    new MoveActorTowardDestinationUseCase(
                        new ActorMovementService(
                            navigationService,
                            actorSpatialIndexService,
                            actorViewDataStore)),
                    new UseDungeonStairOrchestrator(
                        new EnsureDungeonFloorGeneratedOrchestrator(new GenerateDungeonFloorUseCase(new FixedWorldGameSettingsRepository(), new HardcodedMasterRepository(), new AssignDungeonRoomRolesUseCase(new HardcodedMasterRepository())), new NoOpEventPublisher())),
                    new SelectDungeonTargetFloorUseCase(
                        masterRepository,
                        new ActorCombatPowerCalculator(),
                        eventBus),
                    new SelectDungeonExplorationGoalUseCase(1),
                    navigationService,
                    actorCombatService,
                    new GameRandom(),
                    eventBus,
                    new AdventurerExplorationStateService(eventBus),
                    actorSpatialIndexService,
                    actorViewDataStore,
                    TestRuntimeServiceFactory.CreateActorProcessingCandidateService(),
                    new FixedWorldGameSettingsRepository()),
                new DetectCombatEncounterUseCase(
                    actorCombatService,
                    actorSpatialIndexService,
                    new CombatEncounterTargetResolver(gameClock, actorSpatialIndexService, new FixedWorldGameSettingsRepository()),
                    eventBus),
                new AdvanceCombatUseCase(
                    actorCombatService,
                    gameClock,
                    new GameWorldFrameBuffer(),
                    combatEffectExecutor,
                    actorDefeatOrchestrator,
                    eventBus,
                    new ActorMovementService(
                        navigationService,
                        actorSpatialIndexService,
                        actorViewDataStore),
                    new FixedWorldGameSettingsRepository(),
                    new CombatEncounterTargetResolver(
                        gameClock,
                        actorSpatialIndexService,
                        new FixedWorldGameSettingsRepository())),
                new AdvanceProjectileUseCase(combatEffectExecutor, actorDefeatOrchestrator, eventBus, new FixedWorldGameSettingsRepository()),
                new AdvanceAreaEffectUseCase(
                    new AttackAreaTargetResolver(actorSpatialIndexService, new FixedWorldGameSettingsRepository()),
                    combatEffectExecutor,
                    actorDefeatOrchestrator,
                    eventBus),
                new PickUpItemUseCase(
                    eventBus,
                    itemSpatialIndexService,
                    candidateService,
                    new FixedWorldGameSettingsRepository()),
                new UpdateEquipmentUseCase(masterRepository, eventBus, candidateService),
                new SellItemsUseCase(masterRepository, eventBus, gameClock, candidateService),
                new UseRecoveryItemOrchestrator(
                    new UseConsumableItemUseCase(masterRepository, candidateService),
                    eventBus,
                    candidateService,
                    new FixedWorldGameSettingsRepository(),
                    new RecoveryItemCandidateQuery(masterRepository),
                    new RecoveryItemSelectionPolicy()),
                new AdvanceActorEffectsUseCase(candidateService),
                new DecideAdventurerReturnUseCase(
                    actorCombatService,
                    eventBus,
                    new AdventurerReturnTrackingService(eventBus, profileRegistry, achievementRegistry),
                    candidateService,
                    new FixedWorldGameSettingsRepository(),
                    new RecoveryItemCandidateQuery(masterRepository),
                    new RecoveryEffectEstimator()),
                new AdvanceInnRecoveryOrchestrator(
                    new RecoverAdventurerAtInnUseCase(
                        eventBus,
                        gameClock,
                        recoveryStateService,
                        candidateService,
                        new FixedWorldGameSettingsRepository(),
                        new FacilityEffectService()),
                    new ChargeInnFeeUseCase(eventBus, new FixedWorldGameSettingsRepository()),
                    new DespawnAdventurerUseCase(eventBus),
                    eventBus,
                    gameClock,
                    candidateService,
                    new FixedWorldGameSettingsRepository()),
                new PublishInnDailyReportUseCase(
                    worldState,
                    new InnEconomyStatisticsService(eventBus, gameClock),
                    new InnDailyReportStore(),
                    eventBus,
                    new InnEconomyStatusCalculator(new FixedWorldGameSettingsRepository())),
                new FixedWorldGameSettingsRepository(),
                new GameSessionStartRequestStore(),
                new RestoreGameSaveSnapshotUseCase(
                    new NoOpGameClockRestorer(),
                    worldState,
                    worldState,
                    masterRepository,
                    masterRepository,
                    new TutorialProgressService()),
                new ActiveSaveSlotService());
        }

        static GameWorldState CreateWorldState(
            ActorSpatialIndexService actorSpatialIndexService,
            ActorProcessingCandidateService candidateService,
            ActorViewDataStore actorViewDataStore)
        {
            var worldState = new GameWorldState(
                actorSpatialIndexService,
                new ItemSpatialIndexService(new FixedWorldGameSettingsRepository()),
                candidateService,
                actorViewDataStore,
                new FixedWorldGameSettingsRepository());
            worldState.Initialize(CreateGuild(), CreateGroundMap(), CreateDungeon());
            return worldState;
        }

        static AdventurerGuild CreateGuild()
        {
            return new AdventurerGuild(
                Guid.NewGuid(),
                new Inventory(new FixedItemStackLimitResolver()),
                new[]
                {
                    new Facility(
                        Guid.NewGuid(),
                        FacilityType.Inn,
                        "Inn",
                        1,
                        1,
                        new Inventory(new FixedItemStackLimitResolver()))
                });
        }

        static GroundMap CreateGroundMap()
        {
            var layer = new MapLayer(MapLayerId.Ground, 64, 64, 1f);
            var cells = new GroundCell[layer.Width * layer.Depth];
            for (var z = 0; z < layer.Depth; z++)
            {
                for (var x = 0; x < layer.Width; x++)
                {
                    cells[z * layer.Width + x] = new GroundCell(
                        new GridPosition(x, z),
                        GroundCellType.Open,
                        MapCellBlockType.Walkable);
                }
            }

            return new GroundMap(layer, new GridPosition(0, 0), cells);
        }

        static Dungeon CreateDungeon()
        {
            var layer = new MapLayer(MapLayerId.DungeonFloor(1), 80, 20, 1f);
            var cells = new DungeonCell[layer.Width * layer.Depth];
            for (var z = 0; z < layer.Depth; z++)
            {
                for (var x = 0; x < layer.Width; x++)
                {
                    cells[z * layer.Width + x] = new DungeonCell(new GridPosition(x, z), DungeonCellType.Room);
                }
            }

            var floor = new DungeonFloor(
                1,
                layer,
                cells,
                new DungeonStair(DungeonStairType.Up, new GridPosition(1, 1)),
                new DungeonStair(DungeonStairType.Down, new GridPosition(18, 18)),
                Array.Empty<DungeonRoom>(),
                new DungeonFloorGenerationSettings(0));
            var dungeon = new Dungeon(1);
            dungeon.AddFloor(floor);
            return dungeon;
        }

        static Actor CreateActor(int factionId, LayerPosition position)
        {
            return new Actor(
                Guid.NewGuid(),
                0,
                new ActorStats(5, 5, 5, 5, 5, 5),
                new Inventory(new FixedItemStackLimitResolver()),
                1,
                0,
                50,
                10,
                0,
                0,
                1,
                position,
                new ActorFaction(factionId, $"Faction {factionId}"),
                new AdventurerBehavior(0, AdventurerLifecycleState.Exploring),
                WeaponTypeCombatMasterCatalog.Get(WeaponType.Fist));
        }

        sealed class TestGameLoopUseCase : IGameLoopUseCase
        {
            readonly GameLoopTickResult result;

            public TestGameLoopUseCase(GameLoopTickResult result)
            {
                this.result = result;
            }

            public UniTask<GameLoopTickResult> ExecuteAsync(GameLoopTickRequest request)
            {
                return UniTask.FromResult(result);
            }
        }

        sealed class SequenceGameLoopUseCase : IGameLoopUseCase
        {
            readonly Queue<GameLoopTickResult> results = new();

            public SequenceGameLoopUseCase(params GameLoopTickResult[] results)
            {
                foreach (var result in results)
                {
                    this.results.Enqueue(result);
                }
            }

            public UniTask<GameLoopTickResult> ExecuteAsync(GameLoopTickRequest request)
            {
                return UniTask.FromResult(results.Dequeue());
            }
        }

        sealed class DirectNavigationPathProvider : INavigationPathProvider
        {
            readonly List<GridPosition> path = new();

            public IReadOnlyList<LayerPosition> TryFindPath(
                MapLayer layer,
                LayerPosition start,
                LayerPosition goal)
            {
                path.Clear();
                path.Add(layer.ToGridPosition(goal));
                var result = new List<LayerPosition>();
                result.Add(goal);
                return result;
            }
        }

        sealed class StubGameClock : IGameClock
        {
            public int TotalScheduleTick => 0;
            public int CurrentScheduleTick => 0;
            public int CurrentDay => 0;
            public int CurrentTickOfDay => 0;
            public float ElapsedRealTimeSeconds => 0f;
            public float ElapsedGameTimeSeconds => 0f;
            public float TimeScale => 1f;
            public bool IsPaused => true;

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

        sealed class NoOpGameClockRestorer : IGameClockRestorer
        {
            public void Restore(
                int totalScheduleTick,
                float elapsedRealTimeSeconds,
                float elapsedGameTimeSeconds,
                float timeScale,
                bool isPaused)
            {
            }
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


