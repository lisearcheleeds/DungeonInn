using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Domain.Character;
using DungeonInn.Domain.Commerce;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Item;

namespace DungeonInn.Domain.Guild
{
    public sealed class AdventurerGuild
    {
        readonly List<DungeonInn.Domain.Facility.Facility> facilities = new();
        readonly List<GuildStaffAssignment> staffAssignments = new();
        readonly List<ExchangeTransaction> transactions = new();

        public Guid Id { get; }
        public Inventory Inventory { get; }
        public IReadOnlyList<DungeonInn.Domain.Facility.Facility> Facilities => facilities;
        public IReadOnlyList<GuildStaffAssignment> StaffAssignments => staffAssignments;
        public IReadOnlyList<ExchangeTransaction> Transactions => transactions;

        public AdventurerGuild(Guid id, Inventory inventory, IEnumerable<DungeonInn.Domain.Facility.Facility> facilities)
        {
            Id = id;
            Inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            this.facilities.AddRange(facilities ?? throw new ArgumentNullException(nameof(facilities)));
        }

        public DungeonInn.Domain.Facility.Facility GetFacility(Guid facilityId)
        {
            var facility = facilities.FirstOrDefault(x => x.Id.Equals(facilityId));
            if (facility == null)
            {
                throw new InvalidOperationException("Facility does not exist.");
            }

            return facility;
        }

        public void AssignStaff(Character.Character staff, Guid facilityId)
        {
            if (!staff.IsGuildStaff)
            {
                throw new InvalidOperationException("Character is not guild staff.");
            }

            GetFacility(facilityId);
            var assignment = staffAssignments.FirstOrDefault(x => x.StaffId.Equals(staff.Id));
            if (assignment == null)
            {
                staffAssignments.Add(new GuildStaffAssignment(staff.Id, facilityId));
                return;
            }

            assignment.Reassign(facilityId);
        }

        public void RecalculateFacilityPoints(IReadOnlyDictionary<Guid, Character.Character> staffById)
        {
            foreach (var facility in facilities)
            {
                var point = staffAssignments
                    .Where(x => x.FacilityId.Equals(facility.Id))
                    .Select(x => staffById[x.StaffId].CalculateFacilityPoint(facility.Type))
                    .Sum();

                facility.ApplyStaffPoint(point);
            }
        }

        public void RecordTransaction(ExchangeTransaction transaction)
        {
            transactions.Add(transaction ?? throw new ArgumentNullException(nameof(transaction)));
        }
    }
}
