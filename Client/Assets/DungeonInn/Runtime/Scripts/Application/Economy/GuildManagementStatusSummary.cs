using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonInn.Application.Economy
{
    public sealed class GuildManagementStatusSummary
    {
        public GuildManagementStatusSummary(
            GuildEconomyKpiSummary economyKpi,
            IReadOnlyList<FacilityManagementSummary> facilities,
            IReadOnlyList<GuildTransactionHistorySummary> transactions,
            IReadOnlyList<GuildCombinedInventoryItemSummary> combinedInventory)
        {
            EconomyKpi = economyKpi;
            Facilities = (facilities ?? Array.Empty<FacilityManagementSummary>()).ToArray();
            Transactions = (transactions ?? Array.Empty<GuildTransactionHistorySummary>()).ToArray();
            CombinedInventory = (combinedInventory ?? Array.Empty<GuildCombinedInventoryItemSummary>()).ToArray();
        }

        public GuildEconomyKpiSummary EconomyKpi { get; }
        public IReadOnlyList<FacilityManagementSummary> Facilities { get; }
        public IReadOnlyList<GuildTransactionHistorySummary> Transactions { get; }
        public IReadOnlyList<GuildCombinedInventoryItemSummary> CombinedInventory { get; }
    }
}
