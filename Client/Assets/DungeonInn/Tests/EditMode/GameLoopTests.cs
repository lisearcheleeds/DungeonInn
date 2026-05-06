using System;
using DungeonInn.Application.Combat;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.UseCase;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using NUnit.Framework;

namespace DungeonInn.Tests.EditMode
{
    public sealed class GameLoopTests
    {
        [Test]
        public void GameClockReportsDateChangedWhenDayAdvances()
        {
            var clock = new GameClock();
            GameClockAdvanceResult result = null;

            for (var i = 0; i < GameConstants.GameScheduleTicksPerDay; i++)
            {
                result = clock.Advance(1f);
            }

            Assert.That(clock.CurrentDay, Is.EqualTo(1));
            Assert.That(result.GameDateChanged, Is.True);
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
        public void GameLoopAdvancesClockWithoutActorAiEvaluation()
        {
            var useCase = new GameLoopUseCase(new GameClock());

            var result = useCase.ExecuteAsync(new GameLoopTickRequest(0.5f)).GetAwaiter().GetResult();

            Assert.That(result.CurrentScheduleTick, Is.EqualTo(0));
            Assert.That(result.AdvancedScheduleTicks, Is.EqualTo(0));
            Assert.That(result.GameDateChanged, Is.False);
            Assert.That(result.ElapsedRealTimeSeconds, Is.EqualTo(0.5f));
            Assert.That(result.ElapsedGameTimeSeconds, Is.EqualTo(0.5f));
        }

        [Test]
        public void InitializeGameWorldCreatesGroundDungeonGuildAndInn()
        {
            var worldState = new GameWorldState();
            var useCase = new InitializeGameWorldUseCase(
                worldState,
                new InitializeWorldMapUseCase(),
                new InitializeDungeonUseCase(
                    new EnsureDungeonFloorGeneratedUseCase(
                        new GenerateDungeonFloorUseCase())));

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
            var useCase = new AdvanceActorSimpleLifecycleUseCase(
                new MoveActorTowardDestinationUseCase(navigationService),
                new UseDungeonStairUseCase(
                    new EnsureDungeonFloorGeneratedUseCase(
                        new GenerateDungeonFloorUseCase())),
                navigationService,
                new ActorCombatService(),
                new GameRandom(10));
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
            var useCase = new AdvanceActorSimpleLifecycleUseCase(
                new MoveActorTowardDestinationUseCase(navigationService),
                new UseDungeonStairUseCase(
                    new EnsureDungeonFloorGeneratedUseCase(
                        new GenerateDungeonFloorUseCase())),
                navigationService,
                new ActorCombatService(),
                new GameRandom(10));
            var behavior = actor.RequireBehavior<AdventurerBehavior>();

            for (var i = 0; i < 500 && behavior.LifecycleState == AdventurerLifecycleState.Exploring; i++)
            {
                useCase.ExecuteAsync(worldState, 10f).GetAwaiter().GetResult();
            }

            Assert.That(behavior.ExplorationRoomArrivalCount, Is.EqualTo(GameConstants.AdventurerExplorationRoomArrivalTarget));
            Assert.That(behavior.LifecycleState, Is.EqualTo(AdventurerLifecycleState.Returning));
        }

        static GameWorldState CreateInitializedWorldState()
        {
            var worldState = new GameWorldState();
            var useCase = new InitializeGameWorldUseCase(
                worldState,
                new InitializeWorldMapUseCase(),
                new InitializeDungeonUseCase(
                    new EnsureDungeonFloorGeneratedUseCase(
                        new GenerateDungeonFloorUseCase())));

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
            return new Actor(
                Guid.NewGuid(),
                "Exploring Adventurer",
                new ActorStats(5, 5, 5, 5, 5, 5),
                new Inventory(),
                1,
                0,
                50,
                10,
                0,
                0,
                1,
                position,
                new ActorFaction(1, "Adventurer"),
                new AdventurerBehavior(0, AdventurerLifecycleState.Exploring));
        }
    }
}
