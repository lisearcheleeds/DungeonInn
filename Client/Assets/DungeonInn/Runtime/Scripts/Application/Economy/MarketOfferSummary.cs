using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonInn.Application.Economy
{
    public sealed class MarketOfferSummary
    {
        public MarketOfferSummary(
            int offerId,
            IReadOnlyList<MarketOfferRequirementSummary> requirements,
            int rewardGold,
            bool canFulfill)
        {
            if (offerId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(offerId));
            }

            OfferId = offerId;
            Requirements = (requirements ?? Array.Empty<MarketOfferRequirementSummary>()).ToArray();
            RewardGold = Math.Max(0, rewardGold);
            CanFulfill = canFulfill;
        }

        public int OfferId { get; }
        public IReadOnlyList<MarketOfferRequirementSummary> Requirements { get; }
        public int RewardGold { get; }
        public bool CanFulfill { get; }
    }
}
