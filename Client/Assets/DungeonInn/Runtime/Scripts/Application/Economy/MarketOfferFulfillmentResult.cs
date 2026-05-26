using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Domain.Commerce;

namespace DungeonInn.Application.Economy
{
    public sealed class MarketOfferFulfillmentResult
    {
        public MarketOfferFulfillmentResult(
            int offerId,
            int rewardGold,
            GuildInventoryWithdrawalResult withdrawalResult,
            IReadOnlyList<ExchangeTransaction> transactions)
        {
            OfferId = offerId;
            RewardGold = Math.Max(0, rewardGold);
            WithdrawalResult = withdrawalResult ?? throw new ArgumentNullException(nameof(withdrawalResult));
            Transactions = (transactions ?? Array.Empty<ExchangeTransaction>()).ToArray();
        }

        public int OfferId { get; }
        public int RewardGold { get; }
        public GuildInventoryWithdrawalResult WithdrawalResult { get; }
        public IReadOnlyList<ExchangeTransaction> Transactions { get; }
    }
}
