using System.Collections.Generic;

namespace DungeonInn.Master
{
    public interface IMarketOfferMasterRepository
    {
        IReadOnlyDictionary<int, MarketOfferMaster> MarketOfferMasters { get; }

        MarketOfferMaster GetMarketOfferMaster(int offerId);
    }
}
