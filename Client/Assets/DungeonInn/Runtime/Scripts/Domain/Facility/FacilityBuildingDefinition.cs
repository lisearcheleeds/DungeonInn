using System;
using DungeonInn.Domain.Map;

namespace DungeonInn.Domain.Facility
{
    public sealed class FacilityBuildingDefinition
    {
        public FacilityBuildingDefinition(
            FacilityType facilityType,
            GridPosition origin,
            int width,
            int depth,
            GridPosition entranceCell,
            BuildingFacingDirection facingDirection)
        {
            if (width < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (depth < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(depth));
            }

            FacilityType = facilityType;
            Origin = origin;
            Width = width;
            Depth = depth;
            EntranceCell = entranceCell;
            FacingDirection = facingDirection;
        }

        public FacilityType FacilityType { get; }
        public GridPosition Origin { get; }
        public int Width { get; }
        public int Depth { get; }
        public GridPosition EntranceCell { get; }
        public BuildingFacingDirection FacingDirection { get; }

        public bool Contains(GridPosition position)
        {
            return Origin.X <= position.X &&
                position.X < Origin.X + Width &&
                Origin.Z <= position.Z &&
                position.Z < Origin.Z + Depth;
        }
    }
}
