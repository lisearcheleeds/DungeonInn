using System.Collections.Generic;
using DungeonInn.Domain.Facility;

namespace DungeonInn.Master
{
    public interface IFacilityUpgradeMasterRepository
    {
        IReadOnlyDictionary<int, FacilityUpgradeMaster> FacilityUpgradeMasters { get; }

        bool TryGetFacilityUpgradeMaster(FacilityType facilityType, int fromLevel, out FacilityUpgradeMaster master);
    }
}
