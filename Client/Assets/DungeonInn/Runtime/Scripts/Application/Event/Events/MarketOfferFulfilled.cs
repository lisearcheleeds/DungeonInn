using System;

namespace DungeonInn.Application.Event.Events
{
    public sealed class MarketOfferFulfilled : IGameEvent
    {
        public MarketOfferFulfilled(int offerId, int rewardGold)
        {
            OfferId = offerId;
            RewardGold = Math.Max(0, rewardGold);
        }

        public int OfferId { get; }
        public int RewardGold { get; }
    }
}
