using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD.ScreenStack
{
    public sealed class MarketWindowViewData
    {
        public MarketWindowViewData(IReadOnlyList<MarketOfferViewData> offers)
        {
            Offers = (offers ?? Array.Empty<MarketOfferViewData>()).ToArray();
        }

        public IReadOnlyList<MarketOfferViewData> Offers { get; }
    }
}
