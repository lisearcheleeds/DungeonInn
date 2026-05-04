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
        readonly List<InnReservation> innReservations = new();
        readonly List<ExchangeOffer> exchangeOffers = new();
        readonly List<ExchangeTransaction> transactions = new();

        public Guid Id { get; }
        public Inventory Inventory { get; }
        public IReadOnlyList<DungeonInn.Domain.Facility.Facility> Facilities => facilities;
        public IReadOnlyList<GuildStaffAssignment> StaffAssignments => staffAssignments;
        public IReadOnlyList<InnReservation> InnReservations => innReservations;
        public IReadOnlyList<ExchangeOffer> ExchangeOffers => exchangeOffers;
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

        public bool HasActiveInnReservation(Guid adventurerId)
        {
            return innReservations.Any(x => x.IsActive && x.AdventurerId.Equals(adventurerId));
        }

        public int CountActiveInnReservations(Guid innFacilityId)
        {
            return innReservations.Count(x => x.IsActive && x.InnFacilityId.Equals(innFacilityId));
        }

        public bool CanReserveInn(Guid innFacilityId)
        {
            var facility = GetFacility(innFacilityId);
            if (facility.Type != FacilityType.Inn)
            {
                return false;
            }

            return CountActiveInnReservations(innFacilityId) < facility.Capacity;
        }

        public InnReservation ReserveInn(Guid reservationId, Character.Character adventurer, Guid innFacilityId, int occurredAtTick)
        {
            if (adventurer == null)
            {
                throw new ArgumentNullException(nameof(adventurer));
            }

            var facility = GetFacility(innFacilityId);
            if (facility.Type != FacilityType.Inn)
            {
                throw new InvalidOperationException("Facility is not inn.");
            }

            if (HasActiveInnReservation(adventurer.Id))
            {
                throw new InvalidOperationException("Adventurer already has inn reservation.");
            }

            if (facility.Capacity <= CountActiveInnReservations(innFacilityId))
            {
                throw new InvalidOperationException("Inn has no vacant room.");
            }

            var reservation = new InnReservation(
                reservationId,
                adventurer.Id,
                innFacilityId,
                occurredAtTick);

            innReservations.Add(reservation);
            adventurer.MarkResident();
            return reservation;
        }

        public void ReleaseInnReservation(Guid adventurerId, int occurredAtTick)
        {
            var reservation = innReservations.FirstOrDefault(x => x.IsActive && x.AdventurerId.Equals(adventurerId));
            if (reservation == null)
            {
                return;
            }

            reservation.Release(occurredAtTick);
        }

        public void AddExchangeOffer(ExchangeOffer exchangeOffer)
        {
            exchangeOffers.Add(exchangeOffer ?? throw new ArgumentNullException(nameof(exchangeOffer)));
        }

        public void RecordTransaction(ExchangeTransaction transaction)
        {
            transactions.Add(transaction ?? throw new ArgumentNullException(nameof(transaction)));
        }
    }
}
