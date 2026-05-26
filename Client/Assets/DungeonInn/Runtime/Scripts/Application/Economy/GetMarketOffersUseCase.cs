using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Domain.Item;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.Economy
{
    public sealed class GetMarketOffersUseCase
    {
        readonly IMarketOfferMasterRepository marketOfferMasterRepository;
        readonly IItemMasterRepository itemMasterRepository;
        readonly GuildCombinedInventoryViewService combinedInventoryViewService;

        [Inject]
        public GetMarketOffersUseCase(
            IMarketOfferMasterRepository marketOfferMasterRepository,
            IItemMasterRepository itemMasterRepository,
            GuildCombinedInventoryViewService combinedInventoryViewService)
        {
            this.marketOfferMasterRepository = marketOfferMasterRepository
                ?? throw new ArgumentNullException(nameof(marketOfferMasterRepository));
            this.itemMasterRepository = itemMasterRepository ?? throw new ArgumentNullException(nameof(itemMasterRepository));
            this.combinedInventoryViewService =
                combinedInventoryViewService ?? throw new ArgumentNullException(nameof(combinedInventoryViewService));
        }

        public IReadOnlyList<MarketOfferSummary> Execute()
        {
            return marketOfferMasterRepository.MarketOfferMasters.Values
                .OrderBy(x => x.DisplayPriority)
                .Take(3)
                .Select(CreateSummary)
                .ToArray();
        }

        MarketOfferSummary CreateSummary(MarketOfferMaster master)
        {
            var requirements = master.Requirements
                .Select(x => new MarketOfferRequirementSummary(
                    x.ItemId,
                    itemMasterRepository.GetItemMaster(x.ItemId).Name,
                    x.Count,
                    combinedInventoryViewService.CountItem(x.ItemId)))
                .ToArray();
            return new MarketOfferSummary(
                master.Id,
                requirements,
                CountGold(master.Rewards),
                requirements.All(x => x.IsSatisfied));
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
