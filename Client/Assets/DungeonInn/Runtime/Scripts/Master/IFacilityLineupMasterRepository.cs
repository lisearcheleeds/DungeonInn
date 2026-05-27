using System.Collections.Generic;

namespace DungeonInn.Master
{
    public interface IFacilityLineupMasterRepository
    {
        IReadOnlyDictionary<int, FacilityLineupMaster> FacilityLineupMasters { get; }
        IReadOnlyDictionary<int, FacilityLineupItemMaster> FacilityLineupItemMasters { get; }
    }
}
