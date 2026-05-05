using DungeonInn.Application.GameLoop;
using DungeonInn.Application.UseCase;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Item;
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
    }
}
