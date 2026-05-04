using System;

namespace DungeonInn.Domain.Facility
{
    public sealed class InnReservation
    {
        public Guid Id { get; }
        public Guid AdventurerId { get; }
        public Guid InnFacilityId { get; }
        public int ReservedAtTick { get; }
        public bool IsActive { get; private set; }
        public int? ReleasedAtTick { get; private set; }

        public InnReservation(Guid id, Guid adventurerId, Guid innFacilityId, int reservedAtTick)
        {
            Id = id;
            AdventurerId = adventurerId;
            InnFacilityId = innFacilityId;
            ReservedAtTick = Math.Max(0, reservedAtTick);
            IsActive = true;
        }

        public void Release(int occurredAtTick)
        {
            if (!IsActive)
            {
                return;
            }

            IsActive = false;
            ReleasedAtTick = Math.Max(0, occurredAtTick);
        }
    }
}
