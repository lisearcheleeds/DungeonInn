using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Commerce;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Item;
using ActorEntity = DungeonInn.Domain.Actor.Actor;

namespace DungeonInn.Domain.Guild
{
    public sealed class AdventurerGuild
    {
        readonly List<DungeonInn.Domain.Facility.Facility> facilities = new();
        readonly List<GuildStaffAssignment> staffAssignments = new();
        readonly List<InnReservation> innReservations = new();
        readonly Dictionary<Guid, InnReservation> activeReservationByAdventurer = new();
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

        public void AssignStaff(ActorEntity staff, Guid facilityId)
        {
            if (staff.Behavior is not GuildStaffBehavior)
            {
                throw new InvalidOperationException("Actor is not guild staff.");
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

        public void RecalculateFacilityPoints(IReadOnlyDictionary<Guid, ActorEntity> staffById)
        {
            foreach (var facility in facilities)
            {
                var point = staffAssignments
                    .Where(x => x.FacilityId.Equals(facility.Id))
                    .Select(x =>
                    {
                        var staff = staffById[x.StaffId];
                        var behavior = staff.RequireBehavior<GuildStaffBehavior>();
                        return behavior.CalculateFacilityPoint(staff, facility.Type);
                    })
                    .Sum();

                facility.ApplyStaffPoint(point);
            }
        }

        public bool HasActiveInnReservation(Guid adventurerId)
        {
            return activeReservationByAdventurer.ContainsKey(adventurerId);
        }

        public int CountActiveInnReservations(Guid innFacilityId)
        {
            var count = 0;
            foreach (var reservation in activeReservationByAdventurer.Values)
            {
                if (reservation.InnFacilityId.Equals(innFacilityId))
                {
                    count++;
                }
            }

            return count;
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

        public InnReservation ReserveInn(Guid reservationId, ActorEntity adventurer, Guid innFacilityId, int occurredAtTick)
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
            activeReservationByAdventurer[adventurer.Id] = reservation;
            return reservation;
        }

        public void ReleaseInnReservation(Guid adventurerId, int occurredAtTick)
        {
            if (!activeReservationByAdventurer.TryGetValue(adventurerId, out var reservation))
            {
                return;
            }

            reservation.Release(occurredAtTick);
            activeReservationByAdventurer.Remove(adventurerId);
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
