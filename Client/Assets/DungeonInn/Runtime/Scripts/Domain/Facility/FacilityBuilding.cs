using System;
using DungeonInn.Domain.Map;

namespace DungeonInn.Domain.Facility
{
    public sealed class FacilityBuilding
    {
        public FacilityBuilding(
            Guid facilityId,
            FacilityType facilityType,
            FacilityBuildingDefinition definition,
            FacilityInteractionPoint interactionPoint)
        {
            if (facilityId == Guid.Empty)
            {
                throw new ArgumentException("Facility id is required.", nameof(facilityId));
            }

            FacilityId = facilityId;
            FacilityType = facilityType;
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            InteractionPoint = interactionPoint;
        }

        public Guid FacilityId { get; }
        public FacilityType FacilityType { get; }
        public FacilityBuildingDefinition Definition { get; }
        public FacilityInteractionPoint InteractionPoint { get; }

        public bool Contains(GridPosition position)
        {
            return Definition.Contains(position);
        }
    }
}
