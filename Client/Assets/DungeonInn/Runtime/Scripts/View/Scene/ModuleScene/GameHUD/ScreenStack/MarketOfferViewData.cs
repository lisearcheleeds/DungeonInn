using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD.ScreenStack
{
    public sealed class MarketOfferViewData
    {
        public MarketOfferViewData(
            int offerId,
            IReadOnlyList<MarketOfferRequirementViewData> requirements,
            string reward,
            string status,
            bool canFulfill)
        {
            OfferId = offerId;
            Requirements = (requirements ?? Array.Empty<MarketOfferRequirementViewData>()).ToArray();
            Reward = reward ?? string.Empty;
            Status = status ?? string.Empty;
            CanFulfill = canFulfill;
        }

        public int OfferId { get; }
        public IReadOnlyList<MarketOfferRequirementViewData> Requirements { get; }
        public string Reward { get; }
        public string Status { get; }
        public bool CanFulfill { get; }
    }
}
