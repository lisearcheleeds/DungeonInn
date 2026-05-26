using System;
using System.Collections.Generic;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.World;
using DungeonInn.Domain.Commerce;
using DungeonInn.Domain.Item;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.Economy
{
    public sealed class FulfillMarketOfferUseCase
    {
        readonly IGameWorldStateReader worldState;
        readonly IMarketOfferMasterRepository marketOfferMasterRepository;
        readonly GuildCombinedInventoryViewService combinedInventoryViewService;
        readonly GuildInventoryWithdrawalService withdrawalService;
        readonly IGameClock gameClock;
        readonly IEventPublisher eventPublisher;

        [Inject]
        public FulfillMarketOfferUseCase(
            IGameWorldStateReader worldState,
            IMarketOfferMasterRepository marketOfferMasterRepository,
            GuildCombinedInventoryViewService combinedInventoryViewService,
            GuildInventoryWithdrawalService withdrawalService,
            IGameClock gameClock,
            IEventPublisher eventPublisher)
        {
            this.worldState = worldState ?? throw new ArgumentNullException(nameof(worldState));
            this.marketOfferMasterRepository = marketOfferMasterRepository
                ?? throw new ArgumentNullException(nameof(marketOfferMasterRepository));
            this.combinedInventoryViewService =
                combinedInventoryViewService ?? throw new ArgumentNullException(nameof(combinedInventoryViewService));
            this.withdrawalService = withdrawalService ?? throw new ArgumentNullException(nameof(withdrawalService));
            this.gameClock = gameClock ?? throw new ArgumentNullException(nameof(gameClock));
            this.eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        }

        public MarketOfferFulfillmentResult Execute(int offerId)
        {
            var offer = marketOfferMasterRepository.GetMarketOfferMaster(offerId);
            if (!combinedInventoryViewService.HasAll(offer.Requirements))
            {
                throw new InvalidOperationException("Guild combined inventory does not satisfy market offer.");
            }

            var withdrawalResult = withdrawalService.Withdraw(offer.Requirements);
            ((IExchangeParticipant)worldState.Guild).AddRange(offer.Rewards);
            var transactions = RecordTransactions(withdrawalResult);
            var rewardGold = CountGold(offer.Rewards);
            eventPublisher.Publish(new MarketOfferFulfilled(offerId, rewardGold));
            return new MarketOfferFulfillmentResult(offerId, rewardGold, withdrawalResult, transactions);
        }

        IReadOnlyList<ExchangeTransaction> RecordTransactions(
            GuildInventoryWithdrawalResult withdrawalResult)
        {
            var transactions = new List<ExchangeTransaction>();
            foreach (var source in withdrawalResult.Sources)
            {
                var transaction = new ExchangeTransaction(
                    Guid.NewGuid(),
                    source.SourceId,
                    MarketIds.DefaultMarketId,
                    new[] { source.ItemStack },
                    Array.Empty<ItemStack>(),
                    gameClock.CurrentScheduleTick);
                worldState.Guild.RecordTransaction(transaction);
                transactions.Add(transaction);
            }

            return transactions;
        }

        static int CountGold(IReadOnlyList<ItemStack> rewards)
        {
            var total = 0;
            foreach (var reward in rewards)
            {
                if (reward.ItemId == SpecialItemIds.Money)
                {
                    total += reward.Count;
                }
            }

            return total;
        }
    }
}
