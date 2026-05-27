using System;
using System.Collections.Generic;
using VContainer;

namespace DungeonInn.Application.Economy
{
    public sealed class MarketScreenService : IMarketScreenService
    {
        readonly GetMarketOffersUseCase getMarketOffersUseCase;
        readonly FulfillMarketOfferUseCase fulfillMarketOfferUseCase;

        [Inject]
        public MarketScreenService(
            GetMarketOffersUseCase getMarketOffersUseCase,
            FulfillMarketOfferUseCase fulfillMarketOfferUseCase)
        {
            this.getMarketOffersUseCase =
                getMarketOffersUseCase ?? throw new ArgumentNullException(nameof(getMarketOffersUseCase));
            this.fulfillMarketOfferUseCase =
                fulfillMarketOfferUseCase ?? throw new ArgumentNullException(nameof(fulfillMarketOfferUseCase));
        }

        public IReadOnlyList<MarketOfferSummary> GetOffers()
        {
            return getMarketOffersUseCase.Execute();
        }

        public MarketOfferFulfillmentResult FulfillOffer(int offerId)
        {
            return fulfillMarketOfferUseCase.Execute(offerId);
        }
    }
}
