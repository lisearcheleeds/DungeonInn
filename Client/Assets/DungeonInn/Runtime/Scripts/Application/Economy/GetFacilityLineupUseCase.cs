using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Domain.Facility;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.Economy
{
    public sealed class GetFacilityLineupUseCase
    {
        readonly IFacilityLineupMasterRepository facilityLineupMasterRepository;
        readonly IItemMasterRepository itemMasterRepository;

        [Inject]
        public GetFacilityLineupUseCase(
            IFacilityLineupMasterRepository facilityLineupMasterRepository,
            IItemMasterRepository itemMasterRepository)
        {
            this.facilityLineupMasterRepository = facilityLineupMasterRepository
                ?? throw new ArgumentNullException(nameof(facilityLineupMasterRepository));
            this.itemMasterRepository = itemMasterRepository ?? throw new ArgumentNullException(nameof(itemMasterRepository));
        }

        public IReadOnlyList<FacilityLineupSummary> Execute(FacilityType facilityType, int facilityLevel)
        {
            var availableLineupIds = facilityLineupMasterRepository.FacilityLineupMasters.Values
                .Where(x => x.FacilityType == facilityType && x.RequiredLevel <= facilityLevel)
                .OrderBy(x => x.DisplayPriority)
                .ThenBy(x => x.Id)
                .ToDictionary(x => x.Id);

            if (availableLineupIds.Count == 0)
            {
                return Array.Empty<FacilityLineupSummary>();
            }

            return facilityLineupMasterRepository.FacilityLineupItemMasters.Values
                .Where(x => availableLineupIds.ContainsKey(x.LineupId))
                .OrderBy(x => availableLineupIds[x.LineupId].DisplayPriority)
                .ThenBy(x => x.DisplayPriority)
                .ThenBy(x => x.ItemId)
                .Select(x => new FacilityLineupSummary(
                    x.ItemId,
                    itemMasterRepository.GetItemMaster(x.ItemId).Name,
                    availableLineupIds[x.LineupId].RequiredLevel))
                .ToArray();
        }
    }
}
