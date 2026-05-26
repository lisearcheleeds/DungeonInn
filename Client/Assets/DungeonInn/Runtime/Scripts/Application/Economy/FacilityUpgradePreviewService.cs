using System;
using System.Linq;
using DungeonInn.Domain.Facility;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.Economy
{
    public sealed class FacilityUpgradePreviewService
    {
        readonly IFacilityUpgradeMasterRepository facilityUpgradeMasterRepository;
        readonly IItemMasterRepository itemMasterRepository;
        readonly GuildCombinedInventoryViewService combinedInventoryViewService;

        [Inject]
        public FacilityUpgradePreviewService(
            IFacilityUpgradeMasterRepository facilityUpgradeMasterRepository,
            IItemMasterRepository itemMasterRepository,
            GuildCombinedInventoryViewService combinedInventoryViewService)
        {
            this.facilityUpgradeMasterRepository = facilityUpgradeMasterRepository
                ?? throw new ArgumentNullException(nameof(facilityUpgradeMasterRepository));
            this.itemMasterRepository = itemMasterRepository ?? throw new ArgumentNullException(nameof(itemMasterRepository));
            this.combinedInventoryViewService =
                combinedInventoryViewService ?? throw new ArgumentNullException(nameof(combinedInventoryViewService));
        }

        public FacilityUpgradePreviewSummary Create(FacilityType facilityType, int fromLevel)
        {
            if (!facilityUpgradeMasterRepository.TryGetFacilityUpgradeMaster(facilityType, fromLevel, out var master))
            {
                return new FacilityUpgradePreviewSummary(
                    false,
                    false,
                    fromLevel,
                    0,
                    0,
                    0,
                    Array.Empty<FacilityUpgradeCostSummary>(),
                    "No next upgrade");
            }

            var costs = master.Costs
                .Select(x => new FacilityUpgradeCostSummary(
                    x.ItemId,
                    itemMasterRepository.GetItemMaster(x.ItemId).Name,
                    x.Count,
                    combinedInventoryViewService.CountItem(x.ItemId)))
                .ToArray();
            return new FacilityUpgradePreviewSummary(
                true,
                costs.All(x => x.IsSatisfied),
                master.FromLevel,
                master.ToLevel,
                master.Quality,
                master.Capacity,
                costs,
                costs.All(x => x.IsSatisfied) ? string.Empty : "Insufficient materials");
        }
    }
}
