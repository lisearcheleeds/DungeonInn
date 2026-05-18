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
using DungeonInn.Domain.Commerce;
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
    public sealed class InnEconomyUseCaseTests
    {
        static ActorProcessingCandidateService currentCandidateService;

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
        public void InitializedGuildIncludesTradeFacilities()
        {
            var worldState = CreateInitializedWorldState();

            Assert.That(worldState.Guild.Facilities.Any(x => x.Type == FacilityType.Inn), Is.True);
            Assert.That(worldState.Guild.Facilities.Any(x => x.Type == FacilityType.GeneralStore), Is.True);
            Assert.That(worldState.Guild.Facilities.Any(x => x.Type == FacilityType.EquipmentShop), Is.True);
        }

        [Test]
        public void AutomatedItemSaleTransfersItemsToFacilityAndRecordsTransaction()
        {
            const int herbItemId = 1001;
            var worldState = CreateInitializedWorldState();
            var actor = CreateAdventurer(0);
            actor.GainItem(new ItemStack(herbItemId, 2));
            worldState.RegisterActor(actor);
            var eventBus = new CollectingEventBus();
            var clock = new StubGameClock { CurrentScheduleTickValue = 123 };
            var generalStore = worldState.Guild.Facilities.First(x => x.Type == FacilityType.GeneralStore);
            var initialGeneralStoreGold = generalStore.Inventory.Gold;
            var useCase = new SellItemsUseCase(new HardcodedMasterRepository(), eventBus, clock, currentCandidateService);

            useCase.Execute(worldState);

            Assert.That(actor.Inventory.Gold, Is.EqualTo(10));
            Assert.That(actor.Inventory.Has(new ItemStack(herbItemId, 1)), Is.False);
            Assert.That(worldState.Guild.Inventory.Gold, Is.EqualTo(GameConstants.InitialGuildReserveGold));
            Assert.That(generalStore.Inventory.Gold, Is.EqualTo(initialGeneralStoreGold - 10));
            Assert.That(generalStore.Inventory.Has(new ItemStack(herbItemId, 2)), Is.True);
            Assert.That(worldState.Guild.Transactions.Count, Is.EqualTo(1));
            Assert.That(worldState.Guild.Transactions[0].InitiatorId, Is.EqualTo(actor.Id));
            Assert.That(worldState.Guild.Transactions[0].CounterpartyId, Is.EqualTo(generalStore.Id));
            Assert.That(worldState.Guild.Transactions[0].InitiatorItems[0].ItemId, Is.EqualTo(herbItemId));
            Assert.That(worldState.Guild.Transactions[0].CounterpartyItems[0].Count, Is.EqualTo(10));
            Assert.That(worldState.Guild.Transactions[0].OccurredAtTick, Is.EqualTo(123));
            Assert.That(eventBus.GetEvents<ItemSold>().Count, Is.EqualTo(1));
            Assert.That(eventBus.GetEvents<ItemSold>()[0].TotalPrice, Is.EqualTo(10));
        }

        [Test]
        public void AutomatedItemSaleSkipsWhenFacilityCannotPay()
        {
            const int herbItemId = 1001;
            var worldState = CreateInitializedWorldState();
            var generalStore = worldState.Guild.Facilities.First(x => x.Type == FacilityType.GeneralStore);
            ((IExchangeParticipant)generalStore).Remove(new ItemStack(SpecialItemIds.Money, GameConstants.InitialGeneralStoreGold));
            var actor = CreateAdventurer(0);
            actor.GainItem(new ItemStack(herbItemId, 1));
            worldState.RegisterActor(actor);
            var eventBus = new CollectingEventBus();
            var useCase = new SellItemsUseCase(
                new HardcodedMasterRepository(),
                eventBus,
                new StubGameClock(),
                currentCandidateService);

            useCase.Execute(worldState);

            Assert.That(actor.Inventory.Gold, Is.EqualTo(0));
            Assert.That(actor.Inventory.Has(new ItemStack(herbItemId, 1)), Is.True);
            Assert.That(generalStore.Inventory.Has(new ItemStack(herbItemId, 1)), Is.False);
            Assert.That(worldState.Guild.Transactions.Count, Is.EqualTo(0));
            Assert.That(eventBus.GetEvents<ItemSold>().Count, Is.EqualTo(0));
        }

        [Test]
        public void AutomatedItemSaleDoesNotSellEquippedEquipment()
        {
            const int armorItemId = 3003;
            var masterRepository = new HardcodedMasterRepository();
            var worldState = CreateInitializedWorldState();
            var actor = CreateAdventurer(0);
            actor.GainItem(new ItemStack(armorItemId, 1));
            actor.Equip(masterRepository.GetEquipmentMaster(armorItemId));
            worldState.RegisterActor(actor);
            var eventBus = new CollectingEventBus();
            var useCase = new SellItemsUseCase(
                masterRepository,
                eventBus,
                new StubGameClock(),
                currentCandidateService);

            useCase.Execute(worldState);

            Assert.That(actor.Inventory.Gold, Is.EqualTo(0));
            Assert.That(actor.Inventory.Has(new ItemStack(armorItemId, 1)), Is.True);
            Assert.That(CountItem(worldState, armorItemId), Is.EqualTo(GameConstants.InitialRookieArmorCount));
            Assert.That(worldState.Guild.Transactions.Count, Is.EqualTo(0));
            Assert.That(eventBus.GetEvents<ItemSold>().Count, Is.EqualTo(0));
        }

        [Test]
        public void AutomatedItemSaleUsesCandidateActorWhenManyActorsExist()
        {
            const int herbItemId = 1001;
            var worldState = CreateInitializedWorldState();
            for (var i = 0; i < 120; i++)
            {
                worldState.RegisterActor(CreateAdventurer(0, AdventurerLifecycleState.Exploring));
            }

            var actor = CreateAdventurer(0, AdventurerLifecycleState.WaitingForInn);
            actor.GainItem(new ItemStack(herbItemId, 2));
            worldState.RegisterActor(actor);
            var eventBus = new CollectingEventBus();
            var useCase = new SellItemsUseCase(
                new HardcodedMasterRepository(),
                eventBus,
                new StubGameClock(),
                currentCandidateService);

            useCase.Execute(worldState);

            Assert.That(actor.Inventory.Gold, Is.EqualTo(10));
            Assert.That(eventBus.GetEvents<ItemSold>().Count, Is.EqualTo(1));
        }


        [Test]
        public void PublishInnDailyReportSavesSnapshotFromStatistics()
        {
            var worldState = CreateInitializedWorldState();
            var actor = CreateAdventurer(GameConstants.InnFeePerStay);
            worldState.Guild.ReserveInn(Guid.NewGuid(), actor, worldState.Guild.Facilities[0].Id, 1);
            var clock = new StubGameClock { CurrentDayValue = 0 };
            var eventBus = new CollectingEventBus();
            var statisticsService = new InnEconomyStatisticsService(eventBus, clock);
            var reportStore = new InnDailyReportStore();
            new ChargeInnFeeUseCase(eventBus).Execute(actor, worldState.Guild);
            var useCase = new PublishInnDailyReportUseCase(
                worldState,
                statisticsService,
                reportStore,
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
            Assert.That(reportStore.TryGet(0, out var savedReport), Is.True);
            Assert.That(savedReport.Guests, Is.EqualTo(1));
            statisticsService.Dispose();
        }

        [Test]
        public void PublishInnDailyReportDoesNotReplenishRookieEquipment()
        {
            var worldState = CreateInitializedWorldState();
            RemoveStock(worldState, GameConstants.InitialRookieSwordItemId, GameConstants.InitialRookieSwordCount - 2);
            var eventBus = new CollectingEventBus();
            var statisticsService = new InnEconomyStatisticsService(eventBus, new StubGameClock());
            var useCase = new PublishInnDailyReportUseCase(
                worldState,
                statisticsService,
                new InnDailyReportStore(),
                eventBus,
                new InnEconomyStatusCalculator());

            useCase.ExecuteAsync(0).GetAwaiter().GetResult();

            Assert.That(CountItem(worldState, GameConstants.InitialRookieSwordItemId), Is.EqualTo(2));
            Assert.That(eventBus.GetEvents<GuildSupplyReplenished>().Any(x => x.ItemId == GameConstants.InitialRookieSwordItemId), Is.False);
            statisticsService.Dispose();
        }

        [Test]
        public void GetInnEconomyStatusReturnsCurrentDailyCounters()
        {
            var worldState = CreateInitializedWorldState();
            var actor = CreateAdventurer(GameConstants.InnFeePerStay);
            var clock = new StubGameClock { CurrentDayValue = 2 };
            var eventBus = new CollectingEventBus();
            var statisticsService = new InnEconomyStatisticsService(eventBus, clock);
            new ChargeInnFeeUseCase(eventBus).Execute(actor, worldState.Guild);
            var useCase = new GetInnEconomyStatusUseCase(
                worldState,
                clock,
                statisticsService,
                new InnEconomyStatusCalculator());

            var status = useCase.ExecuteAsync().GetAwaiter().GetResult();

            Assert.That(status.CurrentDay, Is.EqualTo(2));
            Assert.That(status.Current.Guests, Is.EqualTo(1));
            Assert.That(status.Current.Demand, Is.EqualTo(1));
            Assert.That(status.Current.Sales, Is.EqualTo(GameConstants.InnFeePerStay));
            Assert.That(status.Current.RoomCapacity, Is.EqualTo(GameConstants.InitialInnCapacity));
            Assert.That(status.Current.GuildGold, Is.EqualTo(GameConstants.InitialGuildGold + GameConstants.InnFeePerStay));
            statisticsService.Dispose();
        }

        [Test]
        public void InnEconomyStatusUsesDailyReportSummaryAsCanonicalSnapshot()
        {
            var worldState = CreateInitializedWorldState();
            var calculator = new InnEconomyStatusCalculator();
            var statistics = new InnEconomyStatistics(2, 1, 30, -1);

            var report = calculator.CalculateDailyReport(worldState, 3, statistics);
            var status = calculator.Calculate(worldState, 3, statistics);

            Assert.That(status.CurrentDay, Is.EqualTo(report.Day));
            Assert.That(status.Current, Is.EqualTo(report.Summary));
            Assert.That(report.Summary.Demand, Is.EqualTo(statistics.Demand));
        }

        [Test]
        public void GetInnEconomyReportReturnsSavedDailyReports()
        {
            var store = new InnDailyReportStore();
            store.Save(CreateReport(1, 1, 0, GameConstants.InnFeePerStay));
            store.Save(CreateReport(2, 0, 1, 0));
            var useCase = new GetInnEconomyReportUseCase(store);

            var result = useCase.TryGetByDayAsync(1).GetAwaiter().GetResult();
            var reports = useCase.GetByDayRangeAsync(1, 2).GetAwaiter().GetResult();

            Assert.That(result.HasValue, Is.True);
            Assert.That(result.Value.Day, Is.EqualTo(1));
            Assert.That(result.Value.Guests, Is.EqualTo(1));
            Assert.That(reports.Count, Is.EqualTo(2));
            Assert.That(reports[1].RejectedGuests, Is.EqualTo(1));
        }

        [Test]
        public void GetInnEconomyReportReturnsNotFoundWhenDailyReportIsMissing()
        {
            var useCase = new GetInnEconomyReportUseCase(new InnDailyReportStore());

            var result = useCase.TryGetByDayAsync(10).GetAwaiter().GetResult();

            Assert.That(result.HasValue, Is.False);
        }

        static GameWorldState CreateInitializedWorldState()
        {
            currentCandidateService = TestRuntimeServiceFactory.CreateActorProcessingCandidateService();
            var worldState = new GameWorldState(
                new ActorSpatialIndexService(),
                new ItemSpatialIndexService(),
                currentCandidateService,
                new ActorViewDataStore());
            var useCase = new InitializeGameWorldOrchestrator(
                worldState,
                new InitializeWorldMapUseCase(),
                new InitializeDungeonOrchestrator(new GenerateDungeonFloorUseCase()),
                new HardcodedMasterRepository(),
                new CollectingEventBus());

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
            return CreateAdventurer(gold, AdventurerLifecycleState.Recovering);
        }

        static Actor CreateAdventurer(int gold, AdventurerLifecycleState lifecycleState)
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

        static void RemoveStock(GameWorldState worldState, int itemId, int count)
        {
            ((IExchangeParticipant)worldState.Guild).Remove(new ItemStack(itemId, count));
        }

        static int CountItem(GameWorldState worldState, int itemId)
        {
            return worldState.Guild.Inventory.ItemCounts.TryGetValue(itemId, out var count) ? count : 0;
        }

        static Domain.Guild.InnDailyReport CreateReport(
            int day,
            int guests,
            int rejectedGuests,
            int sales)
        {
            return new Domain.Guild.InnDailyReport(
                day,
                new Domain.Guild.InnEconomySummary(
                    guests,
                    rejectedGuests,
                    guests + rejectedGuests,
                    sales,
                    0,
                    GameConstants.InitialInnReputation,
                    0,
                    GameConstants.InitialInnCapacity,
                    0,
                    GameConstants.InitialGuildGold,
                    GameConstants.InitialRookieSwordCount,
                    GameConstants.InitialRookieArmorCount));
        }

        sealed class CollectingEventBus : IGameEventBus
        {
            readonly List<IGameEvent> events = new();
            readonly Subject<IGameEvent> subject = new();
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
                subject.OnNext(gameEvent);
            }

            public Observable<T> OnEvent<T>() where T : class, IGameEvent
            {
                return subject.Where(gameEvent => gameEvent is T).Select(gameEvent => (T)(object)gameEvent);
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

            public int TotalScheduleTick => totalScheduleTickValue;
            public int CurrentScheduleTick => totalScheduleTickValue;

            public int CurrentScheduleTickValue
            {
                get => totalScheduleTickValue;
                set => totalScheduleTickValue = value;
            }

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
