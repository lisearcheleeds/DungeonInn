using System;
using DungeonInn.Domain.Facility;

namespace DungeonInn.Master
{
    public sealed class FacilityLineupMaster
    {
        public FacilityLineupMaster(int id, FacilityType facilityType, int requiredLevel, int displayPriority)
        {
            if (id < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            if (requiredLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(requiredLevel));
            }

            Id = id;
            FacilityType = facilityType;
            RequiredLevel = requiredLevel;
            DisplayPriority = Math.Max(0, displayPriority);
        }

        public int Id { get; }
        public FacilityType FacilityType { get; }
        public int RequiredLevel { get; }
        public int DisplayPriority { get; }
    }
}
