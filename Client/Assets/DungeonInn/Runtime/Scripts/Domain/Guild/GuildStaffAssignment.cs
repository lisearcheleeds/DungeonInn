using System;

namespace DungeonInn.Domain.Guild
{
    public sealed class GuildStaffAssignment
    {
        public Guid StaffId { get; }
        public Guid FacilityId { get; private set; }

        public GuildStaffAssignment(Guid staffId, Guid facilityId)
        {
            StaffId = staffId;
            FacilityId = facilityId;
        }

        public void Reassign(Guid facilityId)
        {
            FacilityId = facilityId;
        }
    }
}
