using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.Orchestration;
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
    public sealed class InnEconomyUseCaseTests
    {
        [Test]
        public void ChargeInnFeeRecordsSalesGuestAndSatisfaction()
        {
            var worldState = CreateInitializedWorldState();
            var actor = CreateAdventurer(GameConstants.InnFeePerStay);
            var eventBus = new CollectingEventBus();
            var useCase = new ChargeInnFeeUseCase(eventBus);

            var charged = useCase.Execute(actor, worldState.Guild);

            Assert.That(charged, Is.True);
            Assert.That(eventBus.GetEvents<InnFeeCharged>().Count, Is.EqualTo(1));
            Assert.That(eventBus.GetEvents<InnFeeCharged>()[0].FeeAmount, Is.EqualTo(GameConstants.InnFeePerStay));
            Assert.That(eventBus.GetEvents<InnSatisfactionChanged>().Count, Is.EqualTo(1));
            Assert.That(eventBus.GetEvents<InnSatisfactionChanged>()[0].Delta, Is.EqualTo(GameConstants.InnStayedSatisfactionDelta));
        }

        [Test]
        public void ChargeInnFeeRecordsRejectedGuestWhenActorCannotPay()
        {
            var worldState = CreateInitializedWorldState();
            var actor = CreateAdventurer(0);
            var eventBus = new CollectingEventBus();
            var useCase = new ChargeInnFeeUseCase(eventBus);

            var charged = useCase.Execute(actor, worldState.Guild);

            Assert.That(charged, Is.False);
            Assert.That(eventBus.GetEvents<InnFeeCharged>().Count, Is.EqualTo(0));
            Assert.That(eventBus.GetEvents<InnSatisfactionChanged>().Count, Is.EqualTo(1));
            Assert.That(eventBus.GetEvents<InnSatisfactionChanged>()[0].Reason, Is.EqualTo(InnSatisfactionChangeReason.CannotPayInnFee));
        }

        [Test]
        public void PublishInnDailyReportGeneratesReportFromEventHistory()
        {
            var worldState = CreateInitializedWorldState();
            var actor = CreateAdventurer(GameConstants.InnFeePerStay);
            worldState.Guild.ReserveInn(Guid.NewGuid(), actor, worldState.Guild.Facilities[0].Id, 1);
            var clock = new StubGameClock { CurrentDayValue = 0 };
            var history = new GameEventHistoryService(clock);
            var eventBus = new CollectingEventBus(history);
            new ChargeInnFeeUseCase(eventBus).Execute(actor, worldState.Guild);
            var useCase = new PublishInnDailyReportUseCase(
                worldState,
                history,
                eventBus,
                new InnEconomyStatusCalculator());

            useCase.ExecuteAsync(0).GetAwaiter().GetResult();

            var reports = eventBus.GetEvents<DailyInnReportGenerated>();
            Assert.That(reports.Count, Is.EqualTo(1));
            Assert.That(reports[0].Report.Day, Is.EqualTo(0));
            Assert.That(reports[0].Report.Guests, Is.EqualTo(1));
            Assert.That(reports[0].Report.Sales, Is.EqualTo(GameConstants.InnFeePerStay));
            Assert.That(reports[0].Report.OccupiedRooms, Is.EqualTo(1));
            Assert.That(reports[0].Report.RoomCapacity, Is.EqualTo(GameConstants.InitialInnCapacity));
        }

        [Test]
        public void PublishInnDailyReportDoesNotReplenishRookieEquipment()
        {
            var worldState = CreateInitializedWorldState();
            RemoveStock(worldState, GameConstants.InitialRookieSwordItemId, GameConstants.InitialRookieSwordCount - 2);
            var history = new GameEventHistoryService(new StubGameClock());
            var eventBus = new CollectingEventBus();
            var useCase = new PublishInnDailyReportUseCase(
                worldState,
                history,
                eventBus,
                new InnEconomyStatusCalculator());

            useCase.ExecuteAsync(0).GetAwaiter().GetResult();

            Assert.That(CountItem(worldState, GameConstants.InitialRookieSwordItemId), Is.EqualTo(2));
            Assert.That(eventBus.GetEvents<GuildSupplyReplenished>().Any(x => x.ItemId == GameConstants.InitialRookieSwordItemId), Is.False);
        }

        [Test]
        public void GetInnEconomyStatusReturnsCurrentDailyCounters()
        {
            var worldState = CreateInitializedWorldState();
            var actor = CreateAdventurer(GameConstants.InnFeePerStay);
            var clock = new StubGameClock { CurrentDayValue = 2 };
            var history = new GameEventHistoryService(clock);
            new ChargeInnFeeUseCase(new CollectingEventBus(history)).Execute(actor, worldState.Guild);
            var useCase = new GetInnEconomyStatusUseCase(
                worldState,
                clock,
                history,
                new InnEconomyStatusCalculator());

            var status = useCase.ExecuteAsync().GetAwaiter().GetResult();

            Assert.That(status.CurrentDay, Is.EqualTo(2));
            Assert.That(status.GuestsToday, Is.EqualTo(1));
            Assert.That(status.DemandToday, Is.EqualTo(1));
            Assert.That(status.SalesToday, Is.EqualTo(GameConstants.InnFeePerStay));
            Assert.That(status.RoomCapacity, Is.EqualTo(GameConstants.InitialInnCapacity));
            Assert.That(status.GuildGold, Is.EqualTo(GameConstants.InitialGuildGold + GameConstants.InnFeePerStay));
        }

        [Test]
        public void GetInnEconomyReportReturnsDayRangeStatistics()
        {
            var worldState = CreateInitializedWorldState();
            var clock = new StubGameClock { CurrentDayValue = 1 };
            var history = new GameEventHistoryService(clock);
            var eventBus = new CollectingEventBus(history);
            new ChargeInnFeeUseCase(eventBus).Execute(
                CreateAdventurer(GameConstants.InnFeePerStay),
                worldState.Guild);
            clock.CurrentDayValue = 2;
            new ChargeInnFeeUseCase(eventBus).Execute(
                CreateAdventurer(0),
                worldState.Guild);
            var useCase = new GetInnEconomyReportUseCase(
                worldState,
                history,
                new InnEconomyStatusCalculator());

            var report = useCase.GetByDayRangeAsync(1, 2).GetAwaiter().GetResult();

            Assert.That(report.StartDay, Is.EqualTo(1));
            Assert.That(report.EndDay, Is.EqualTo(2));
            Assert.That(report.Guests, Is.EqualTo(1));
            Assert.That(report.RejectedGuests, Is.EqualTo(1));
            Assert.That(report.Demand, Is.EqualTo(2));
            Assert.That(report.Sales, Is.EqualTo(GameConstants.InnFeePerStay));
            Assert.That(report.SatisfactionDelta, Is.EqualTo(
                GameConstants.InnStayedSatisfactionDelta + GameConstants.InnCannotPaySatisfactionDelta));
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

        static Actor CreateAdventurer(int gold)
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
                new AdventurerBehavior(0, AdventurerLifecycleState.Recovering));
        }

        static void RemoveStock(GameWorldState worldState, int itemId, int count)
        {
            worldState.Guild.Inventory.Remove(new ItemStack(itemId, count));
        }

        static int CountItem(GameWorldState worldState, int itemId)
        {
            return worldState.Guild.Inventory.ItemCounts.TryGetValue(itemId, out var count) ? count : 0;
        }

        sealed class CollectingEventBus : IGameEventBus
        {
            readonly List<IGameEvent> events = new();
            readonly IGameEventHistoryRecorder historyRecorder;

            public CollectingEventBus()
            {
            }

            public CollectingEventBus(IGameEventHistoryRecorder historyRecorder)
            {
                this.historyRecorder = historyRecorder;
            }

            public void Publish(IGameEvent gameEvent)
            {
                historyRecorder?.Record(gameEvent);
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

            public int CurrentScheduleTick => 0;
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
