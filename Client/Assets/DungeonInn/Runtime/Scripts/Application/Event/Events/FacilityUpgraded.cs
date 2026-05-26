using System;
using DungeonInn.Domain.Facility;

namespace DungeonInn.Application.Event.Events
{
    public sealed class FacilityUpgraded : IGameEvent
    {
        public FacilityUpgraded(Guid facilityId, FacilityType facilityType, int previousLevel, int newLevel)
        {
            FacilityId = facilityId;
            FacilityType = facilityType;
            PreviousLevel = previousLevel;
            NewLevel = newLevel;
        }

        public Guid FacilityId { get; }
        public FacilityType FacilityType { get; }
        public int PreviousLevel { get; }
        public int NewLevel { get; }
    }
}
