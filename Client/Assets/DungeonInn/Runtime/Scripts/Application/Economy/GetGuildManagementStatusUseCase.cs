using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.World;
using DungeonInn.Domain.Item;
using VContainer;

namespace DungeonInn.Application.Economy
{
    public sealed class GetGuildManagementStatusUseCase
    {
        readonly IGameWorldStateReader worldState;
        readonly IGameClock gameClock;
        readonly InnEconomyStatisticsService statisticsService;
        readonly InnEconomyStatusCalculator economyStatusCalculator;
        readonly GuildCombinedInventoryViewService combinedInventoryViewService;
        readonly FacilityUpgradePreviewService facilityUpgradePreviewService;
        readonly GetFacilityLineupUseCase getFacilityLineupUseCase;

        [Inject]
        public GetGuildManagementStatusUseCase(
            IGameWorldStateReader worldState,
            IGameClock gameClock,
            InnEconomyStatisticsService statisticsService,
            InnEconomyStatusCalculator economyStatusCalculator,
            GuildCombinedInventoryViewService combinedInventoryViewService,
            FacilityUpgradePreviewService facilityUpgradePreviewService,
            GetFacilityLineupUseCase getFacilityLineupUseCase)
        {
            this.worldState = worldState ?? throw new ArgumentNullException(nameof(worldState));
            this.gameClock = gameClock ?? throw new ArgumentNullException(nameof(gameClock));
            this.statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
            this.economyStatusCalculator =
                economyStatusCalculator ?? throw new ArgumentNullException(nameof(economyStatusCalculator));
            this.combinedInventoryViewService =
                combinedInventoryViewService ?? throw new ArgumentNullException(nameof(combinedInventoryViewService));
            this.facilityUpgradePreviewService =
                facilityUpgradePreviewService ?? throw new ArgumentNullException(nameof(facilityUpgradePreviewService));
            this.getFacilityLineupUseCase =
                getFacilityLineupUseCase ?? throw new ArgumentNullException(nameof(getFacilityLineupUseCase));
        }

        public bool CanExecute => worldState.IsInitialized;

        public GuildManagementStatusSummary Execute()
        {
            if (!CanExecute)
            {
                return new GuildManagementStatusSummary(
                    default,
                    Array.Empty<FacilityManagementSummary>(),
                    Array.Empty<GuildTransactionHistorySummary>(),
                    Array.Empty<GuildCombinedInventoryItemSummary>());
            }

            var economy = economyStatusCalculator
                .Calculate(worldState, gameClock.CurrentDay, statisticsService.GetByDay(gameClock.CurrentDay))
                .Current;
            return new GuildManagementStatusSummary(
                new GuildEconomyKpiSummary(
                    economy.GuildGold,
                    economy.Sales,
                    economy.Guests,
                    economy.OccupancyPercent,
                    economy.Reputation),
                CreateFacilities(),
                CreateTransactions(),
                combinedInventoryViewService.GetSnapshot());
        }

        IReadOnlyList<FacilityManagementSummary> CreateFacilities()
        {
            return worldState.Guild.Facilities
                .OrderBy(x => x.Type)
                .Select(x => new FacilityManagementSummary(
                    x.Id,
                    x.Type,
                    x.Name,
                    x.Level,
                    x.Quality,
                    x.Capacity,
                    x.Inventory.Gold,
                    CalculateFacilitySales(x.Id),
                    facilityUpgradePreviewService.Create(x.Type, x.Level),
                    getFacilityLineupUseCase.Execute(x.Type, x.Level)))
                .ToArray();
        }

        int CalculateFacilitySales(Guid facilityId)
        {
            var total = 0;
            foreach (var transaction in worldState.Guild.Transactions)
            {
                if (!transaction.CounterpartyId.Equals(facilityId))
                {
                    continue;
                }

                total += CountGold(transaction.InitiatorItems);
            }

            return total;
        }

        static int CountGold(IReadOnlyList<ItemStack> items)
        {
            var total = 0;
            foreach (var item in items)
            {
                if (item.ItemId == SpecialItemIds.Money)
                {
                    total += item.Count;
                }
            }

            return total;
        }

        IReadOnlyList<GuildTransactionHistorySummary> CreateTransactions()
        {
            return worldState.Guild.Transactions
                .OrderByDescending(x => x.OccurredAtTick)
                .Take(12)
                .Select(x => new GuildTransactionHistorySummary(
                    x.OccurredAtTick,
                    $"Tick {x.OccurredAtTick}: {x.InitiatorItems.Count} item groups for {x.CounterpartyItems.Count} item groups"))
                .ToArray();
        }
    }
}
