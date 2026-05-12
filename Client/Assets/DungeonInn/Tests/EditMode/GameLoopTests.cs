using System;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Event;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.Orchestration;
using DungeonInn.Application.UseCase;
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

            scaleUseCase.ExecuteAsync(4f).GetAwaiter().GetResult();
            pauseUseCase.ExecuteAsync().GetAwaiter().GetResult();
            clock.Advance(1f);
            var pausedState = queryUseCase.ExecuteAsync().GetAwaiter().GetResult();
            resumeUseCase.ExecuteAsync().GetAwaiter().GetResult();
            clock.Advance(1f);
            var resumedState = queryUseCase.ExecuteAsync().GetAwaiter().GetResult();

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

            var pausedState = useCase.ExecuteAsync().GetAwaiter().GetResult();
            var resumedState = useCase.ExecuteAsync().GetAwaiter().GetResult();

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
            var worldState = new GameWorldState();
            var useCase = new InitializeGameWorldOrchestrator(
                worldState,
                new InitializeWorldMapUseCase(),
                new InitializeDungeonOrchestrator(
                    new EnsureDungeonFloorGeneratedOrchestrator(
                        new GenerateDungeonFloorUseCase())),
                new HardcodedMasterRepository());

            var result = useCase.ExecuteAsync(
                    new InitializeGameWorldRequest(
                        GameConstants.InitialDungeonSeed,
                        System.Array.Empty<DungeonDepthBandConfig>()))
                .GetAwaiter()
                .GetResult();

            Assert.That(result.IsInitialized, Is.True);
            Assert.That(result.GroundMap, Is.Not.Null);
            Assert.That(result.Dungeon, Is.Not.Null);
            Assert.That(result.Dungeon.HasFloor(1), Is.True);
            Assert.That(result.Guild, Is.Not.Null);
            Assert.That(result.Guild.Facilities.Count, Is.EqualTo(1));
            Assert.That(result.Guild.Facilities[0].Type, Is.EqualTo(FacilityType.Inn));
            Assert.That(result.Guild.Inventory.HasAll(new[] { new ItemStack(SpecialItemIds.Money, GameConstants.InitialGuildGold) }), Is.True);
        }

        [Test]
        public void ExploringAdventurerMovesTowardRandomDungeonRoom()
        {
            var worldState = CreateInitializedWorldState();
            var floor = worldState.Dungeon.GetFloor(1);
            var actor = CreateExploringAdventurer(floor.GetArrivalPosition(DungeonStairType.Up));
            worldState.RegisterActor(actor);

            var navigationService = new ActorNavigationService();
            var useCase = new AdvanceActorLifecycleOrchestrator(
                new MoveActorTowardDestinationUseCase(navigationService),
                new UseDungeonStairOrchestrator(
                    new EnsureDungeonFloorGeneratedOrchestrator(
                        new GenerateDungeonFloorUseCase())),
                CreateSelectDungeonTargetFloorUseCase(),
                new SelectDungeonExplorationGoalUseCase(1),
                navigationService,
                new ActorCombatService(),
                new GameRandom(10),
                new NoOpGameEventBus(),
                new AdventurerExplorationStateService(new NoOpGameEventBus()));
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
        public void ExploringAdventurerReturnsAfterArrivingAtEightRoomDestinations()
        {
            var worldState = CreateInitializedWorldState();
            var floor = worldState.Dungeon.GetFloor(1);
            var actor = CreateExploringAdventurer(floor.GetArrivalPosition(DungeonStairType.Up));
            worldState.RegisterActor(actor);

            var navigationService = new ActorNavigationService();
            var useCase = new AdvanceActorLifecycleOrchestrator(
                new MoveActorTowardDestinationUseCase(navigationService),
                new UseDungeonStairOrchestrator(
                    new EnsureDungeonFloorGeneratedOrchestrator(
                        new GenerateDungeonFloorUseCase())),
                CreateSelectDungeonTargetFloorUseCase(),
                new SelectDungeonExplorationGoalUseCase(1),
                navigationService,
                new ActorCombatService(),
                new GameRandom(10),
                new NoOpGameEventBus(),
                new AdventurerExplorationStateService(new NoOpGameEventBus()));
            var behavior = actor.RequireBehavior<AdventurerBehavior>();

            for (var i = 0; i < 500 && behavior.LifecycleState == AdventurerLifecycleState.Exploring; i++)
            {
                useCase.ExecuteAsync(worldState, 10f).GetAwaiter().GetResult();
            }

            Assert.That(behavior.ExplorationRoomArrivalCount, Is.EqualTo(GameConstants.AdventurerExplorationRoomArrivalTarget));
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
        public void ReturningAdventurerAscendsToPreviousFloorBeforeGround()
        {
            var worldState = CreateInitializedWorldState();
            var floorGenerator = new EnsureDungeonFloorGeneratedOrchestrator(new GenerateDungeonFloorUseCase());
            var secondFloor = floorGenerator.ExecuteAsync(
                    worldState.Dungeon,
                    2,
                    Array.Empty<DungeonDepthBandConfig>())
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

        static GameWorldState CreateInitializedWorldState()
        {
            var worldState = new GameWorldState();
            var useCase = new InitializeGameWorldOrchestrator(
                worldState,
                new InitializeWorldMapUseCase(),
                new InitializeDungeonOrchestrator(
                    new EnsureDungeonFloorGeneratedOrchestrator(
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
                new AdventurerBehavior(0, lifecycleState));
        }

        static AdvanceActorLifecycleOrchestrator CreateLifecycleUseCase()
        {
            var navigationService = new ActorNavigationService();
            return new AdvanceActorLifecycleOrchestrator(
                new MoveActorTowardDestinationUseCase(navigationService),
                new UseDungeonStairOrchestrator(
                    new EnsureDungeonFloorGeneratedOrchestrator(
                        new GenerateDungeonFloorUseCase())),
                CreateSelectDungeonTargetFloorUseCase(),
                new SelectDungeonExplorationGoalUseCase(1),
                navigationService,
                new ActorCombatService(),
                new GameRandom(10),
                new NoOpGameEventBus(),
                new AdventurerExplorationStateService(new NoOpGameEventBus()));
        }

        static SelectDungeonTargetFloorUseCase CreateSelectDungeonTargetFloorUseCase()
        {
            var masterRepository = new HardcodedMasterRepository();
            return new SelectDungeonTargetFloorUseCase(
                masterRepository,
                new ActorCombatPowerCalculator(masterRepository),
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
    }
}
