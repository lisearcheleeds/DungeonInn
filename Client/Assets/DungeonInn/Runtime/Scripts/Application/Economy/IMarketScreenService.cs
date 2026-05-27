using System.Collections.Generic;

namespace DungeonInn.Application.Economy
{
    public interface IMarketScreenService
    {
        IReadOnlyList<MarketOfferSummary> GetOffers();

        MarketOfferFulfillmentResult FulfillOffer(int offerId);
    }
}
