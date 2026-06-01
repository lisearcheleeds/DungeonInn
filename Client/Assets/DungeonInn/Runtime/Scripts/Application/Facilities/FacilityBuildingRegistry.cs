using System;
using System.Collections.Generic;
using DungeonInn.Domain.Facility;

namespace DungeonInn.Application.Facilities
{
    public sealed class FacilityBuildingRegistry
    {
        readonly Dictionary<Guid, FacilityBuilding> buildingByFacilityId = new();
        readonly Dictionary<FacilityType, FacilityBuilding> buildingByType = new();

        public void Clear()
        {
            buildingByFacilityId.Clear();
            buildingByType.Clear();
        }

        public void Register(FacilityBuilding building)
        {
            if (building == null)
            {
                throw new ArgumentNullException(nameof(building));
            }

            buildingByFacilityId[building.FacilityId] = building;
            buildingByType[building.FacilityType] = building;
        }

        public bool TryGetByFacilityId(Guid facilityId, out FacilityBuilding building)
        {
            return buildingByFacilityId.TryGetValue(facilityId, out building);
        }

        public bool TryGetByType(FacilityType facilityType, out FacilityBuilding building)
        {
            return buildingByType.TryGetValue(facilityType, out building);
        }
    }
}
