using System;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Map;
using UnityEngine;

namespace DungeonInn.GameSession.Settings
{
    [Serializable]
    public sealed class FacilityBuildingSettings
    {
        [SerializeField] FacilityType facilityType;
        [SerializeField] int originX;
        [SerializeField] int originZ;
        [SerializeField] int width = 5;
        [SerializeField] int depth = 5;
        [SerializeField] int entranceX;
        [SerializeField] int entranceZ;
        [SerializeField] BuildingFacingDirection facingDirection = BuildingFacingDirection.South;

        public FacilityBuildingSettings()
        {
        }

        public FacilityBuildingSettings(
            FacilityType facilityType,
            int originX,
            int originZ,
            int width,
            int depth,
            int entranceX,
            int entranceZ,
            BuildingFacingDirection facingDirection)
        {
            this.facilityType = facilityType;
            this.originX = originX;
            this.originZ = originZ;
            this.width = width;
            this.depth = depth;
            this.entranceX = entranceX;
            this.entranceZ = entranceZ;
            this.facingDirection = facingDirection;
        }

        public FacilityBuildingDefinition ToDefinition()
        {
            return new FacilityBuildingDefinition(
                facilityType,
                new GridPosition(originX, originZ),
                width,
                depth,
                new GridPosition(entranceX, entranceZ),
                facingDirection);
        }
    }
}
