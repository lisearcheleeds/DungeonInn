using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Domain.Commerce;

namespace DungeonInn.Application.Economy
{
    public sealed class FacilityUpgradeResult
    {
        public FacilityUpgradeResult(
            Guid facilityId,
            int previousLevel,
            int newLevel,
            GuildInventoryWithdrawalResult withdrawalResult,
            IReadOnlyList<ExchangeTransaction> transactions)
        {
            FacilityId = facilityId;
            PreviousLevel = previousLevel;
            NewLevel = newLevel;
            WithdrawalResult = withdrawalResult ?? throw new ArgumentNullException(nameof(withdrawalResult));
            Transactions = (transactions ?? Array.Empty<ExchangeTransaction>()).ToArray();
        }

        public Guid FacilityId { get; }
        public int PreviousLevel { get; }
        public int NewLevel { get; }
        public GuildInventoryWithdrawalResult WithdrawalResult { get; }
        public IReadOnlyList<ExchangeTransaction> Transactions { get; }
    }
}
