using System;
using DungeonInn.Domain.Map;

namespace DungeonInn.Domain.Facility
{
    public readonly struct FacilityInteractionPoint
    {
        public FacilityInteractionPoint(LayerPosition position, float arrivalRadiusMeters)
        {
            if (arrivalRadiusMeters <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(arrivalRadiusMeters));
            }

            Position = position;
            ArrivalRadiusMeters = arrivalRadiusMeters;
        }

        public LayerPosition Position { get; }
        public float ArrivalRadiusMeters { get; }
    }
}
