using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Event;
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
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using DungeonInn.Master;
using NUnit.Framework;
using R3;

namespace DungeonInn.Tests.EditMode
{
    public sealed class GameLoopTests
    {
        static readonly InitialWorldSettings InitialWorld = InitialWorldSettings.CreateDefault();
        static readonly ActorSimulationSettings ActorSimulation = ActorSimulationSettings.CreateDefault();

        [Test]
        public void GameClockReportsCompletedDayWhenDayBoundaryIsCrossed()
        {
            var clock = new GameClock();
            var result = default(GameClockAdvanceResult);

            for (var i = 0; i < GameConstants.GameScheduleTicksPerDay; i++)
            {
                result = clock.Advance(1f);
            }

            Assert.That(clock.CurrentDay, Is.EqualTo(1));
            Assert.That(result.DayBoundaryCrossed, Is.True);
            Assert.That(result.CompletedDays, Is.EqualTo(new[] { 0 }));
        }

        [Test]
        public void GameClockReportsMultipleCompletedDaysWhenLargeDeltaCrossesBoundaries()
        {
            var clock = new GameClock();

            var result = clock.Advance(GameConstants.GameScheduleTicksPerDay * 2f);

            Assert.That(clock.TotalScheduleTick, Is.EqualTo(GameConstants.GameScheduleTicksPerDay * 2));
            Assert.That(clock.CurrentDay, Is.EqualTo(2));
            Assert.That(result.CompletedDays, Is.EqualTo(new[] { 0, 1 }));
        }

        [Test]
        public void GameClockUsesDedicatedTimeScale()
        {
            var clock = new GameClock();
            clock.SetTimeScale(2f);

            var result = clock.Advance(0.5f);

            Assert.That(clock.ElapsedRealTimeSeconds, Is.EqualTo(0.5f));
            Assert.That(clock.ElapsedGameTimeSeconds, Is.EqualTo(1f));
            Assert.That(clock.CurrentScheduleTick, Is.EqualTo(1));
            Assert.That(result.AdvancedScheduleTicks, Is.EqualTo(1));
            Assert.That(clock.TimeScale, Is.EqualTo(2f));
        }

        [Test]
        public void GameClockDoesNotAdvanceGameTimeWhilePaused()
        {
            var clock = new GameClock();
            clock.Pause();

            var result = clock.Advance(10f);

            Assert.That(clock.ElapsedRealTimeSeconds, Is.EqualTo(10f));
            Assert.That(clock.ElapsedGameTimeSeconds, Is.EqualTo(0f));
            Assert.That(clock.CurrentScheduleTick, Is.EqualTo(0));
            Assert.That(result.AdvancedScheduleTicks, Is.EqualTo(0));
            Assert.That(clock.IsPaused, Is.True);
        }

        [Test]
        public void GameClockResumesAfterPause()
        {
            var clock = new GameClock();
            clock.Pause();
            clock.Advance(10f);
            clock.Resume();

            var result = clock.Advance(1f);

            Assert.That(clock.ElapsedRealTimeSeconds, Is.EqualTo(11f));
            Assert.That(clock.ElapsedGameTimeSeconds, Is.EqualTo(1f));
            Assert.That(clock.CurrentScheduleTick, Is.EqualTo(1));
            Assert.That(result.AdvancedScheduleTicks, Is.EqualTo(1));
            Assert.That(clock.IsPaused, Is.False);
        }

        [Test]
        public void GameTimeUseCasesPauseResumeScaleAndQueryClock()
        {
            var clock = new GameClock();
            var pauseUseCase = new PauseGameTimeUseCase(clock);
            var resumeUseCase = new ResumeGameTimeUseCase(clock);
            var scaleUseCase = new SetGameTimeScaleUseCase(clock);
            var queryUseCase = new GetGameTimeStateUseCase(clock);

            scaleUseCase.Execute(4f);
            pauseUseCase.ExecuteAsync().GetAwaiter().GetResult();
            clock.Advance(1f);
            var pausedState = queryUseCase.Execute();
            resumeUseCase.ExecuteAsync().GetAwaiter().GetResult();
            clock.Advance(1f);
            var resumedState = queryUseCase.Execute();

            Assert.That(pausedState.IsPaused, Is.True);
            Assert.That(pausedState.TimeScale, Is.EqualTo(4f));
            Assert.That(pausedState.ElapsedGameTimeSeconds, Is.EqualTo(0f));
            Assert.That(resumedState.IsPaused, Is.False);
            Assert.That(resumedState.ElapsedGameTimeSeconds, Is.EqualTo(4f));
            Assert.That(resumedState.CurrentScheduleTick, Is.EqualTo(4));
        }

        [Test]
        public void ToggleGamePauseUseCaseAlternatesPauseAndResume()
        {
            var clock = new GameClock();
            var useCase = new ToggleGamePauseUseCase(clock);

            var pausedState = useCase.Execute();
            var resumedState = useCase.Execute();

            Assert.That(pausedState.IsPaused, Is.True);
            Assert.That(clock.IsPaused, Is.False);
            Assert.That(resumedState.IsPaused, Is.False);
        }

        [Test]
        public void GameLoopAdvancesClockWithoutActorAiEvaluation()
        {
            var useCase = new GameLoopUseCase(new GameClock());

            var result = useCase.ExecuteAsync(new GameLoopTickRequest(0.5f)).GetAwaiter().GetResult();

            Assert.That(result.CurrentScheduleTick, Is.EqualTo(0));
            Assert.That(result.AdvancedScheduleTicks, Is.EqualTo(0));
            Assert.That(result.DayBoundaryCrossed, Is.False);
            Assert.That(result.ElapsedRealTimeSeconds, Is.EqualTo(0.5f));
            Assert.That(result.ElapsedGameTimeSeconds, Is.EqualTo(0.5f));
            Assert.That(result.IsPaused, Is.False);
        }

        [Test]
        public void GameLoopReportsPausedStateAndDoesNotAdvanceGameTime()
        {
            var clock = new GameClock();
            clock.Pause();
            var useCase = new GameLoopUseCase(clock);

            var result = useCase.ExecuteAsync(new GameLoopTickRequest(1f)).GetAwaiter().GetResult();

            Assert.That(result.IsPaused, Is.True);
            Assert.That(result.ElapsedRealTimeSeconds, Is.EqualTo(1f));
            Assert.That(result.ElapsedGameTimeSeconds, Is.EqualTo(0f));
            Assert.That(result.AdvancedScheduleTicks, Is.EqualTo(0));
        }

        [Test]
        public void InitializeGameWorldCreatesGroundDungeonGuildAndInn()
        {
            var worldState = CreateWorldState();
            var eventBus = new NoOpGameEventBus();
            var useCase = new InitializeGameWorldOrchestrator(
                worldState,
                new InitializeWorldMapUseCase(new FixedWorldGameSettingsRepository()),
                new InitializeDungeonOrchestrator(new GenerateDungeonFloorUseCase(new FixedWorldGameSettingsRepository(), new HardcodedMasterRepository(), new AssignDungeonRoomRolesUseCase(new HardcodedMasterRepository()))),
                new HardcodedMasterRepository(),
                eventBus,
                new FixedWorldGameSettingsRepository());

            var result = useCase.ExecuteAsync(
                    new InitializeGameWorldRequest(InitialWorld.DungeonSeed))
                .GetAwaiter()
                .GetResult();

            Assert.That(result.IsInitialized, Is.True);
            Assert.That(result.GroundMap, Is.Not.Null);
            Assert.That(result.Dungeon, Is.Not.Null);
            Assert.That(result.Dungeon.HasFloor(1), Is.True);
            Assert.That(result.Guild, Is.Not.Null);
            Assert.That(result.Guild.Facilities.Count, Is.EqualTo(3));
            Assert.That(result.Guild.Facilities.Any(x => x.Type == FacilityType.Inn), Is.True);
            Assert.That(result.Guild.Facilities.Any(x => x.Type == FacilityType.GeneralStore), Is.True);
            Assert.That(result.Guild.Facilities.Any(x => x.Type == FacilityType.EquipmentShop), Is.True);
            Assert.That(result.Guild.Inventory.HasAll(new[] { new ItemStack(SpecialItemIds.Money, InitialWorld.GuildReserveGold) }), Is.True);
            Assert.That(result.Guild.Facilities.First(x => x.Type == FacilityType.GeneralStore).Inventory.Gold, Is.EqualTo(InitialWorld.GeneralStoreGold));
            Assert.That(result.Guild.Facilities.First(x => x.Type == FacilityType.EquipmentShop).Inventory.Gold, Is.EqualTo(InitialWorld.EquipmentShopGold));
        }

        [Test]
        public void ExploringAdventurerMovesTowardRandomDungeonRoom()
        {
            var worldState = CreateInitializedWorldState();
            var floor = worldState.Dungeon.GetFloor(1);
            var actor = CreateExploringAdventurer(floor.GetArrivalPosition(DungeonStairType.Up));
            worldState.RegisterActor(actor);

            var navigationService = new ActorNavigationService(
                new NoOpGameEventBus(),
                new NoOpNavigationPathProvider());
            var spatialIndex = new ActorSpatialIndexService(new FixedWorldGameSettingsRepository());
            var actorViewDataStore = ActorViewDataStoreTestFactory.Create();
            var useCase = new AdvanceActorLifecycleOrchestrator(
                new MoveActorTowardDestinationUseCase(
                    new ActorMovementService(navigationService, spatialIndex, actorViewDataStore)),
                new UseDungeonStairOrchestrator(
                    new EnsureDungeonFloorGeneratedOrchestrator(new GenerateDungeonFloorUseCase(new FixedWorldGameSettingsRepository(), new HardcodedMasterRepository(), new AssignDungeonRoomRolesUseCase(new HardcodedMasterRepository())), new NoOpEventPublisher())),
                CreateSelectDungeonTargetFloorUseCase(),
                new SelectDungeonExplorationGoalUseCase(1),
                navigationService,
                new ActorCombatService(),
                new GameRandom(),
                new NoOpGameEventBus(),
                new AdventurerExplorationStateService(new NoOpGameEventBus()),
                spatialIndex,
                actorViewDataStore,
                TestRuntimeServiceFactory.CreateActorProcessingCandidateService(),
                new FixedWorldGameSettingsRepository());
            var before = actor.Position;

            for (var i = 0; i < 10 && actor.Position.DistanceSquaredTo(before) <= 0f; i++)
            {
                useCase.ExecuteAsync(worldState, 1f).GetAwaiter().GetResult();
            }

            Assert.That(actor.Behavior, Is.TypeOf<AdventurerBehavior>());
            Assert.That(actor.RequireBehavior<AdventurerBehavior>().LifecycleState, Is.EqualTo(AdventurerLifecycleState.Exploring));
            Assert.That(actor.Position.DistanceSquaredTo(before), Is.GreaterThan(0f));
        }

        [Test]
        public void ActorLifecycleAdvanceScopeMovesOnlyIncludedLayer()
        {
            var worldState = CreateInitializedWorldState();
            var floorGenerator = new EnsureDungeonFloorGeneratedOrchestrator(
                new GenerateDungeonFloorUseCase(new FixedWorldGameSettingsRepository(), new HardcodedMasterRepository(), new AssignDungeonRoomRolesUseCase(new HardcodedMasterRepository())),
                new NoOpEventPublisher());
            var firstFloor = worldState.Dungeon.GetFloor(1);
            var secondFloor = floorGenerator.ExecuteAsync(
                    worldState.Dungeon,
                    2)
                .GetAwaiter()
                .GetResult();
            var activeActor = CreateExploringAdventurer(firstFloor.GetArrivalPosition(DungeonStairType.Up));
            var inactiveActor = CreateExploringAdventurer(secondFloor.GetArrivalPosition(DungeonStairType.Up));
            worldState.RegisterActor(activeActor);
            worldState.RegisterActor(inactiveActor);

            var useCase = CreateLifecycleUseCase();
            var activeBefore = activeActor.Position;
            var inactiveBefore = inactiveActor.Position;

            for (var i = 0; i < 10 && activeActor.Position.DistanceSquaredTo(activeBefore) <= 0f; i++)
            {
                useCase.ExecuteAsync(
                        worldState,
                        1f,
                        ActorLifecycleAdvanceScope.Only(firstFloor.Layer.Id))
                    .GetAwaiter()
                    .GetResult();
            }

            Assert.That(activeActor.Position.DistanceSquaredTo(activeBefore), Is.GreaterThan(0f));
            Assert.That(inactiveActor.Position, Is.EqualTo(inactiveBefore));
        }

        [Test]
        public void ExploringAdventurerReturnsAfterArrivingAtEightRoomDestinations()
        {
            var worldState = CreateInitializedWorldState();
            var floor = worldState.Dungeon.GetFloor(1);
            var actor = CreateExploringAdventurer(floor.GetArrivalPosition(DungeonStairType.Up));
            worldState.RegisterActor(actor);

            var navigationService = new ActorNavigationService(
                new NoOpGameEventBus(),
                new NoOpNavigationPathProvider());
            var spatialIndex = new ActorSpatialIndexService(new FixedWorldGameSettingsRepository());
            var actorViewDataStore = ActorViewDataStoreTestFactory.Create();
            var useCase = new AdvanceActorLifecycleOrchestrator(
                new MoveActorTowardDestinationUseCase(
                    new ActorMovementService(navigationService, spatialIndex, actorViewDataStore)),
                new UseDungeonStairOrchestrator(
                    new EnsureDungeonFloorGeneratedOrchestrator(new GenerateDungeonFloorUseCase(new FixedWorldGameSettingsRepository(), new HardcodedMasterRepository(), new AssignDungeonRoomRolesUseCase(new HardcodedMasterRepository())), new NoOpEventPublisher())),
                CreateSelectDungeonTargetFloorUseCase(),
                new SelectDungeonExplorationGoalUseCase(1),
                navigationService,
                new ActorCombatService(),
                new GameRandom(),
                new NoOpGameEventBus(),
                new AdventurerExplorationStateService(new NoOpGameEventBus()),
                spatialIndex,
                actorViewDataStore,
                TestRuntimeServiceFactory.CreateActorProcessingCandidateService(),
                new FixedWorldGameSettingsRepository());
            var behavior = actor.RequireBehavior<AdventurerBehavior>();

            for (var i = 0; i < 500 && behavior.LifecycleState == AdventurerLifecycleState.Exploring; i++)
            {
                useCase.ExecuteAsync(worldState, 10f).GetAwaiter().GetResult();
            }

            Assert.That(behavior.ExplorationRoomArrivalCount, Is.EqualTo(ActorSimulation.ExplorationRoomArrivalTarget));
            Assert.That(behavior.LifecycleState, Is.EqualTo(AdventurerLifecycleState.Returning));
        }

        [Test]
        public void ExploringAdventurerDescendsTowardTargetFloor()
        {
            var worldState = CreateInitializedWorldState();
            var floor = worldState.Dungeon.GetFloor(1);
            var actor = CreateExploringAdventurer(floor.GetArrivalPosition(DungeonStairType.Down));
            var behavior = actor.RequireBehavior<AdventurerBehavior>();
            behavior.SetTargetFloorDepth(2);
            worldState.RegisterActor(actor);

            var useCase = CreateLifecycleUseCase();

            useCase.ExecuteAsync(worldState, 0f).GetAwaiter().GetResult();

            Assert.That(worldState.Dungeon.HasFloor(2), Is.True);
            Assert.That(actor.Position.LayerId, Is.EqualTo(MapLayerId.DungeonFloor(2)));
            Assert.That(behavior.LifecycleState, Is.EqualTo(AdventurerLifecycleState.Exploring));
        }

        [Test]
        public void GoingToDungeonAdventurerUsesEntranceAfterEnteringHalfTileArrivalRadius()
        {
            var worldState = CreateInitializedWorldState();
            var entrance = worldState.GroundMap.Layer.GetCellCenter(worldState.GroundMap.DungeonEntrancePosition);
            var actor = CreateAdventurer(
                new LayerPosition(MapLayerId.Ground, entrance.X - 2.6f, entrance.Z),
                AdventurerLifecycleState.GoingToDungeon);
            worldState.RegisterActor(actor);

            var useCase = CreateLifecycleUseCase();

            for (var i = 0; i < 10 && actor.Position.LayerId.Equals(MapLayerId.Ground); i++)
            {
                useCase.ExecuteAsync(worldState, 0.1f).GetAwaiter().GetResult();
            }

            Assert.That(actor.Position.LayerId, Is.EqualTo(MapLayerId.DungeonFloor(1)));
        }

        [Test]
        public void ReturningAdventurerAscendsToPreviousFloorBeforeGround()
        {
            var worldState = CreateInitializedWorldState();
            var floorGenerator = new EnsureDungeonFloorGeneratedOrchestrator(new GenerateDungeonFloorUseCase(new FixedWorldGameSettingsRepository(), new HardcodedMasterRepository(), new AssignDungeonRoomRolesUseCase(new HardcodedMasterRepository())), new NoOpEventPublisher());
            var secondFloor = floorGenerator.ExecuteAsync(
                    worldState.Dungeon,
                    2)
                .GetAwaiter()
                .GetResult();
            var actor = CreateAdventurer(
                secondFloor.GetArrivalPosition(DungeonStairType.Up),
                AdventurerLifecycleState.Returning);
            var behavior = actor.RequireBehavior<AdventurerBehavior>();
            worldState.RegisterActor(actor);

            var useCase = CreateLifecycleUseCase();

            useCase.ExecuteAsync(worldState, 0f).GetAwaiter().GetResult();

            Assert.That(actor.Position.LayerId, Is.EqualTo(MapLayerId.DungeonFloor(1)));
            Assert.That(behavior.LifecycleState, Is.EqualTo(AdventurerLifecycleState.Returning));
        }

        [Test]
        public void ReturningAdventurerDoesNotMoveTowardStairsWhileCombatTargetExists()
        {
            var worldState = CreateInitializedWorldState();
            var floor = worldState.Dungeon.GetFloor(1);
            var actor = CreateAdventurer(
                floor.GetArrivalPosition(DungeonStairType.Down),
                AdventurerLifecycleState.Returning);
            var monster = CreateMonster(floor.GetArrivalPosition(DungeonStairType.Up));
            worldState.RegisterActor(actor);
            worldState.RegisterActor(monster);

            var combatService = new ActorCombatService();
            combatService.SetTarget(actor.Id, monster.Id);
            var useCase = CreateLifecycleUseCase(combatService);
            var before = actor.Position;

            useCase.ExecuteAsync(worldState, 1f).GetAwaiter().GetResult();

            Assert.That(actor.Position, Is.EqualTo(before));
            Assert.That(combatService.HasTarget(actor.Id), Is.True);
        }

        [Test]
        public void EnsureDungeonFloorGeneratedPublishesMapLayerAddedEventForNewFloor()
        {
            var dungeon = new Dungeon(123);
            var eventPublisher = new CapturingEventPublisher();
            var orchestrator = new EnsureDungeonFloorGeneratedOrchestrator(
                new GenerateDungeonFloorUseCase(new FixedWorldGameSettingsRepository(), new HardcodedMasterRepository(), new AssignDungeonRoomRolesUseCase(new HardcodedMasterRepository())),
                eventPublisher);

            orchestrator.ExecuteAsync(
                    dungeon,
                    2)
                .GetAwaiter()
                .GetResult();

            var mapLayerEvent = eventPublisher.Events.OfType<MapLayerAddedEvent>().Single();
            Assert.That(mapLayerEvent.LayerId, Is.EqualTo(MapLayerId.DungeonFloor(2)));
        }

        [Test]
        public void ActorNavigationServiceKeepsActorPathWhenSearchBufferIsReused()
        {
            var service = new ActorNavigationService(
                new NoOpGameEventBus(),
                new NoOpNavigationPathProvider());
            var layer = new MapLayer(MapLayerId.Ground, 4, 4, 1f);

            var firstState = service.GetOrComputePathState(
                Guid.NewGuid(),
                layer,
                AlwaysWalkableGrid.Instance,
                layer.GetCellCenter(new GridPosition(0, 0)),
                layer.GetCellCenter(new GridPosition(2, 0)));
            var secondState = service.GetOrComputePathState(
                Guid.NewGuid(),
                layer,
                AlwaysWalkableGrid.Instance,
                layer.GetCellCenter(new GridPosition(0, 0)),
                layer.GetCellCenter(new GridPosition(0, 2)));

            Assert.That(firstState.TryGetCurrentWaypoint(out var firstWaypoint), Is.True);
            Assert.That(secondState.TryGetCurrentWaypoint(out var secondWaypoint), Is.True);
            Assert.That(firstWaypoint, Is.EqualTo(layer.GetCellCenter(new GridPosition(1, 0))));
            Assert.That(secondWaypoint, Is.EqualTo(layer.GetCellCenter(new GridPosition(0, 1))));
        }

        [Test]
        public void ActorNavigationServiceRecomputesPathWhenActorMovesOffCachedPath()
        {
            var service = new ActorNavigationService(
                new NoOpGameEventBus(),
                new NoOpNavigationPathProvider());
            var actorId = Guid.NewGuid();
            var layer = new MapLayer(MapLayerId.DungeonFloor(1), 5, 5, 1f);
            var firstState = service.GetOrComputePathState(
                actorId,
                layer,
                AlwaysWalkableGrid.Instance,
                layer.GetCellCenter(new GridPosition(0, 0)),
                layer.GetCellCenter(new GridPosition(4, 0)));

            Assert.That(firstState.TryGetCurrentWaypoint(out var firstWaypoint), Is.True);
            Assert.That(firstWaypoint, Is.EqualTo(layer.GetCellCenter(new GridPosition(1, 0))));

            var secondState = service.GetOrComputePathState(
                actorId,
                layer,
                AlwaysWalkableGrid.Instance,
                layer.GetCellCenter(new GridPosition(0, 4)),
                layer.GetCellCenter(new GridPosition(4, 0)));

            Assert.That(secondState, Is.SameAs(firstState));
            Assert.That(secondState.TryGetCurrentWaypoint(out var secondWaypoint), Is.True);
            Assert.That(secondWaypoint, Is.EqualTo(layer.GetCellCenter(new GridPosition(0, 3))));
        }

        [Test]
        public void ActorNavigationServiceUsesProviderPathWithoutGridValidation()
        {
            var service = new ActorNavigationService(
                new NoOpGameEventBus(),
                new StaticNavigationPathProvider(new[] { new GridPosition(4, 1) }));
            var layer = new MapLayer(MapLayerId.DungeonFloor(1), 5, 3, 1f);
            var actorId = Guid.NewGuid();
            var walkability = new BlockedGridWalkability(new GridPosition(2, 1));

            var state = service.GetOrComputePathState(
                actorId,
                layer,
                walkability,
                layer.GetCellCenter(new GridPosition(0, 1)),
                layer.GetCellCenter(new GridPosition(4, 1)));

            Assert.That(state.HasFailed, Is.False);
            Assert.That(state.TryGetCurrentWaypoint(out var waypoint), Is.True);
            Assert.That(waypoint, Is.EqualTo(layer.GetCellCenter(new GridPosition(4, 1))));
        }

        [Test]
        public void ActorNavigationServiceFallsBackWhenProviderReturnsNull()
        {
            var service = new ActorNavigationService(
                new NoOpGameEventBus(),
                new NoOpNavigationPathProvider());
            var layer = new MapLayer(MapLayerId.DungeonFloor(1), 5, 3, 1f);
            var actorId = Guid.NewGuid();
            var walkability = new BlockedGridWalkability(new GridPosition(1, 1));

            var state = service.GetOrComputePathState(
                actorId,
                layer,
                walkability,
                layer.GetCellCenter(new GridPosition(0, 1)),
                layer.GetCellCenter(new GridPosition(4, 1)));

            Assert.That(state.HasFailed, Is.False);
            Assert.That(state.TryGetCurrentWaypoint(out var waypoint), Is.True);
            Assert.That(waypoint, Is.EqualTo(layer.GetCellCenter(new GridPosition(0, 0))));
        }

        [Test]
        public void ActorNavigationServiceRecomputesLayerPathsAfterLayerInvalidation()
        {
            var provider = new ToggleNavigationPathProvider(new[] { new GridPosition(4, 1) });
            var service = new ActorNavigationService(
                new NoOpGameEventBus(),
                provider);
            var layer = new MapLayer(MapLayerId.DungeonFloor(1), 5, 3, 1f);
            var actorId = Guid.NewGuid();
            var walkability = new BlockedGridWalkability(new GridPosition(1, 1));
            var start = layer.GetCellCenter(new GridPosition(0, 1));
            var goal = layer.GetCellCenter(new GridPosition(4, 1));

            var firstState = service.GetOrComputePathState(
                actorId,
                layer,
                walkability,
                start,
                goal);

            Assert.That(firstState.TryGetCurrentWaypoint(out var firstWaypoint), Is.True);
            Assert.That(firstWaypoint, Is.EqualTo(layer.GetCellCenter(new GridPosition(0, 0))));

            provider.IsAvailable = true;
            service.InvalidateLayerPaths(layer.Id);
            var secondState = service.GetOrComputePathState(
                actorId,
                layer,
                walkability,
                start,
                goal);

            Assert.That(secondState, Is.SameAs(firstState));
            Assert.That(secondState.TryGetCurrentWaypoint(out var secondWaypoint), Is.True);
            Assert.That(secondWaypoint, Is.EqualTo(layer.GetCellCenter(new GridPosition(4, 1))));
        }

        [Test]
        public void ActorNavigationServiceRetriesFailedPathAfterRecheckInterval()
        {
            var service = new ActorNavigationService(
                new NoOpGameEventBus(),
                new NoOpNavigationPathProvider());
            var actorId = Guid.NewGuid();
            var layer = new MapLayer(MapLayerId.DungeonFloor(1), 3, 1, 1f);
            var walkability = new ToggleGoalWalkability(new GridPosition(2, 0));
            var state = service.GetOrComputePathState(
                actorId,
                layer,
                walkability,
                layer.GetCellCenter(new GridPosition(0, 0)),
                layer.GetCellCenter(new GridPosition(2, 0)));

            Assert.That(state.HasFailed, Is.True);

            walkability.IsGoalWalkable = true;
            for (var i = 0; i < 31 && state.HasFailed; i++)
            {
                service.GetOrComputePathState(
                    actorId,
                    layer,
                    walkability,
                    layer.GetCellCenter(new GridPosition(0, 0)),
                    layer.GetCellCenter(new GridPosition(2, 0)));
            }

            Assert.That(state.HasFailed, Is.False);
            Assert.That(state.TryGetCurrentWaypoint(out var waypoint), Is.True);
            Assert.That(waypoint, Is.EqualTo(layer.GetCellCenter(new GridPosition(1, 0))));
        }

        [Test]
        public void MoveActorTowardDestinationUseCaseUsesHalfTileArrivalRadius()
        {
            var navigationService = new ActorNavigationService(
                new NoOpGameEventBus(),
                new NoOpNavigationPathProvider());
            var spatialIndex = new ActorSpatialIndexService(new FixedWorldGameSettingsRepository());
            var actorViewDataStore = ActorViewDataStoreTestFactory.Create();
            var useCase = new MoveActorTowardDestinationUseCase(
                new ActorMovementService(navigationService, spatialIndex, actorViewDataStore));
            var layer = new MapLayer(MapLayerId.DungeonFloor(1), 4, 4, 4f);
            var destination = layer.GetCellCenter(new GridPosition(1, 1));
            var arrivalPosition = new LayerPosition(
                layer.Id,
                destination.X - 1.9f,
                destination.Z);
            var actor = CreateMonster(new LayerPosition(
                layer.Id,
                arrivalPosition.X,
                arrivalPosition.Z));

            var arrived = useCase.Execute(
                actor,
                destination,
                layer,
                AlwaysWalkableGrid.Instance,
                speedMetersPerSecond: 0f,
                deltaGameSeconds: 0f);

            Assert.That(arrived, Is.True);
            Assert.That(actor.Position, Is.EqualTo(arrivalPosition));

            actor = CreateMonster(new LayerPosition(
                layer.Id,
                destination.X - 2.1f,
                destination.Z));
            arrived = useCase.Execute(
                actor,
                destination,
                layer,
                AlwaysWalkableGrid.Instance,
                speedMetersPerSecond: 0f,
                deltaGameSeconds: 0f);

            Assert.That(arrived, Is.False);
        }

        [Test]
        public void MoveActorTowardDestinationUseCaseStopsAtArrivalRadiusWithoutSnappingToDestination()
        {
            var navigationService = new ActorNavigationService(
                new NoOpGameEventBus(),
                new StaticNavigationPathProvider(new[] { new GridPosition(1, 1) }));
            var spatialIndex = new ActorSpatialIndexService(new FixedWorldGameSettingsRepository());
            var actorViewDataStore = ActorViewDataStoreTestFactory.Create();
            var useCase = new MoveActorTowardDestinationUseCase(
                new ActorMovementService(navigationService, spatialIndex, actorViewDataStore));
            var layer = new MapLayer(MapLayerId.DungeonFloor(1), 4, 4, 4f);
            var destination = layer.GetCellCenter(new GridPosition(1, 1));
            var actor = CreateMonster(new LayerPosition(
                layer.Id,
                destination.X - 3f,
                destination.Z));

            var arrived = useCase.Execute(
                actor,
                destination,
                layer,
                AlwaysWalkableGrid.Instance,
                speedMetersPerSecond: 100f,
                deltaGameSeconds: 1f);

            Assert.That(arrived, Is.True);
            Assert.That(actor.Position.X, Is.EqualTo(destination.X - 1.999f).Within(0.0001f));
            Assert.That(actor.Position.Z, Is.EqualTo(destination.Z).Within(0.0001f));
        }

        static GameWorldState CreateInitializedWorldState()
        {
            var worldState = CreateWorldState();
            var useCase = new InitializeGameWorldOrchestrator(
                worldState,
                new InitializeWorldMapUseCase(new FixedWorldGameSettingsRepository()),
                new InitializeDungeonOrchestrator(new GenerateDungeonFloorUseCase(new FixedWorldGameSettingsRepository(), new HardcodedMasterRepository(), new AssignDungeonRoomRolesUseCase(new HardcodedMasterRepository()))),
                new HardcodedMasterRepository(),
                new NoOpGameEventBus(),
                new FixedWorldGameSettingsRepository());

            useCase.ExecuteAsync(
                    new InitializeGameWorldRequest(InitialWorld.DungeonSeed))
                .GetAwaiter()
                .GetResult();

            return worldState;
        }

        static GameWorldState CreateWorldState()
        {
            return new GameWorldState(
                new ActorSpatialIndexService(new FixedWorldGameSettingsRepository()),
                new ItemSpatialIndexService(new FixedWorldGameSettingsRepository()),
                TestRuntimeServiceFactory.CreateActorProcessingCandidateService(),
                ActorViewDataStoreTestFactory.Create(),
                new FixedWorldGameSettingsRepository());
        }

        static Actor CreateExploringAdventurer(LayerPosition position)
        {
            return CreateAdventurer(position, AdventurerLifecycleState.Exploring);
        }

        static Actor CreateAdventurer(
            LayerPosition position,
            AdventurerLifecycleState lifecycleState)
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
                new ActorFaction(1, "Adventurer"),
                new AdventurerBehavior(0, lifecycleState),
                WeaponTypeCombatMasterCatalog.Get(WeaponType.Fist));
        }

        static Actor CreateMonster(LayerPosition position)
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
                new ActorFaction(2, "Monster"),
                new MonsterBehavior(1),
                WeaponTypeCombatMasterCatalog.Get(WeaponType.Fist));
        }

        static AdvanceActorLifecycleOrchestrator CreateLifecycleUseCase()
        {
            return CreateLifecycleUseCase(new ActorCombatService());
        }

        static AdvanceActorLifecycleOrchestrator CreateLifecycleUseCase(IActorCombatService combatService)
        {
            var navigationService = new ActorNavigationService(
                new NoOpGameEventBus(),
                new NoOpNavigationPathProvider());
            var spatialIndex = new ActorSpatialIndexService(new FixedWorldGameSettingsRepository());
            var actorViewDataStore = ActorViewDataStoreTestFactory.Create();
            return new AdvanceActorLifecycleOrchestrator(
                new MoveActorTowardDestinationUseCase(
                    new ActorMovementService(navigationService, spatialIndex, actorViewDataStore)),
                new UseDungeonStairOrchestrator(
                    new EnsureDungeonFloorGeneratedOrchestrator(new GenerateDungeonFloorUseCase(new FixedWorldGameSettingsRepository(), new HardcodedMasterRepository(), new AssignDungeonRoomRolesUseCase(new HardcodedMasterRepository())), new NoOpEventPublisher())),
                CreateSelectDungeonTargetFloorUseCase(),
                new SelectDungeonExplorationGoalUseCase(1),
                navigationService,
                combatService,
                new GameRandom(),
                new NoOpGameEventBus(),
                new AdventurerExplorationStateService(new NoOpGameEventBus()),
                spatialIndex,
                actorViewDataStore,
                TestRuntimeServiceFactory.CreateActorProcessingCandidateService(),
                new FixedWorldGameSettingsRepository());
        }

        static SelectDungeonTargetFloorUseCase CreateSelectDungeonTargetFloorUseCase()
        {
            var masterRepository = new HardcodedMasterRepository();
            return new SelectDungeonTargetFloorUseCase(
                masterRepository,
                new ActorCombatPowerCalculator(),
                new NoOpGameEventBus());
        }

        sealed class NoOpGameEventBus : IGameEventBus
        {
            public void Publish(IGameEvent gameEvent)
            {
            }

            public Observable<T> OnEvent<T>() where T : class, IGameEvent
            {
                return Observable.Empty<T>();
            }
        }

        sealed class CapturingEventPublisher : IEventPublisher
        {
            readonly List<IGameEvent> events = new();

            public IReadOnlyList<IGameEvent> Events => events;

            public void Publish(IGameEvent gameEvent)
            {
                events.Add(gameEvent);
            }
        }

        sealed class AlwaysWalkableGrid : IGridWalkability
        {
            public static readonly AlwaysWalkableGrid Instance = new();

            public bool IsWalkable(GridPosition position)
            {
                return true;
            }
        }

        sealed class BlockedGridWalkability : IGridWalkability
        {
            readonly GridPosition blockedPosition;

            public BlockedGridWalkability(GridPosition blockedPosition)
            {
                this.blockedPosition = blockedPosition;
            }

            public bool IsWalkable(GridPosition position)
            {
                return !position.Equals(blockedPosition);
            }
        }

        sealed class StaticNavigationPathProvider : INavigationPathProvider
        {
            readonly IReadOnlyList<GridPosition> path;

            public StaticNavigationPathProvider(IReadOnlyList<GridPosition> path)
            {
                this.path = path;
            }

            public IReadOnlyList<LayerPosition> TryFindPath(
                MapLayer layer,
                LayerPosition start,
                LayerPosition goal)
            {
                var results = new List<LayerPosition>();
                for (var i = 0; i < path.Count; i++)
                {
                    results.Add(layer.GetCellCenter(path[i]));
                }

                return results;
            }
        }

        sealed class ToggleNavigationPathProvider : INavigationPathProvider
        {
            readonly IReadOnlyList<GridPosition> path;

            public ToggleNavigationPathProvider(IReadOnlyList<GridPosition> path)
            {
                this.path = path;
            }

            public bool IsAvailable { get; set; }

            public IReadOnlyList<LayerPosition> TryFindPath(
                MapLayer layer,
                LayerPosition start,
                LayerPosition goal)
            {
                if (!IsAvailable)
                {
                    return null;
                }

                var results = new List<LayerPosition>();
                for (var i = 0; i < path.Count; i++)
                {
                    results.Add(layer.GetCellCenter(path[i]));
                }

                return results;
            }
        }

        sealed class ToggleGoalWalkability : IGridWalkability
        {
            readonly GridPosition goal;

            public bool IsGoalWalkable { get; set; }

            public ToggleGoalWalkability(GridPosition goal)
            {
                this.goal = goal;
            }

            public bool IsWalkable(GridPosition position)
            {
                return !position.Equals(goal) || IsGoalWalkable;
            }
        }
    }
}


