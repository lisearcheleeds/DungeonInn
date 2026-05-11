using System.Collections.Generic;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Facility;

namespace DungeonInn.Application.GameLoop
{
    public sealed class InnEconomyStatusCalculator
    {
        public InnEconomyStatus Calculate(
            IGameWorldStateReader worldState,
            int currentDay,
            IReadOnlyList<GameEventHistoryEntry> historyEntries)
        {
            var report = CalculateReport(
                worldState,
                currentDay,
                currentDay,
                historyEntries);

            return new InnEconomyStatus(
                currentDay,
                report.Guests,
                report.RejectedGuests,
                report.Demand,
                report.Sales,
                report.SatisfactionDelta,
                report.Reputation,
                report.OccupiedRooms,
                report.RoomCapacity,
                report.OccupancyPercent,
                report.GuildGold,
                report.RookieSwordStock,
                report.RookieArmorStock);
        }

        public InnEconomyReport CalculateReport(
            IGameWorldStateReader worldState,
            int startDay,
            int endDay,
            IReadOnlyList<GameEventHistoryEntry> historyEntries)
        {
            var economy = worldState.InnEconomy;
            var statistics = CalculateStatistics(historyEntries);
            var roomCapacity = CountRoomCapacity(worldState);
            var occupiedRooms = CountOccupiedRooms(worldState);
            var occupancyPercent = roomCapacity <= 0 ? 0 : occupiedRooms * 100 / roomCapacity;

            return new InnEconomyReport(
                startDay,
                endDay,
                statistics.Guests,
                statistics.RejectedGuests,
                statistics.Guests + statistics.RejectedGuests,
                statistics.Sales,
                statistics.SatisfactionDelta,
                economy.Reputation,
                occupiedRooms,
                roomCapacity,
                occupancyPercent,
                worldState.Guild.Inventory.Gold,
                CountItem(worldState, GameConstants.InitialRookieSwordItemId),
                CountItem(worldState, GameConstants.InitialRookieArmorItemId));
        }

        static InnEconomyStatistics CalculateStatistics(IReadOnlyList<GameEventHistoryEntry> historyEntries)
        {
            var guests = 0;
            var rejectedGuests = 0;
            var sales = 0;
            var satisfactionDelta = 0;

            foreach (var entry in historyEntries)
            {
                switch (entry.Event)
                {
                    case InnFeeCharged innFeeCharged:
                        guests++;
                        sales += innFeeCharged.FeeAmount;
                        break;
                    case InnSatisfactionChanged satisfactionChanged:
                        satisfactionDelta += satisfactionChanged.Delta;
                        if (satisfactionChanged.Reason == InnSatisfactionChangeReason.WaitingForInn ||
                            satisfactionChanged.Reason == InnSatisfactionChangeReason.CannotPayInnFee)
                        {
                            rejectedGuests++;
                        }

                        break;
                }
            }

            return new InnEconomyStatistics(
                guests,
                rejectedGuests,
                sales,
                satisfactionDelta);
        }

        static int CountItem(IGameWorldStateReader worldState, int itemId)
        {
            return worldState.Guild.Inventory.ItemCounts.TryGetValue(itemId, out var count) ? count : 0;
        }

        static int CountRoomCapacity(IGameWorldStateReader worldState)
        {
            var capacity = 0;
            foreach (var facility in worldState.Guild.Facilities)
            {
                if (facility.Type == FacilityType.Inn)
                {
                    capacity += facility.Capacity;
                }
            }

            return capacity;
        }

        static int CountOccupiedRooms(IGameWorldStateReader worldState)
        {
            var occupiedRooms = 0;
            foreach (var facility in worldState.Guild.Facilities)
            {
                if (facility.Type == FacilityType.Inn)
                {
                    occupiedRooms += worldState.Guild.CountActiveInnReservations(facility.Id);
                }
            }

            return occupiedRooms;
        }

        readonly struct InnEconomyStatistics
        {
            public int Guests { get; }
            public int RejectedGuests { get; }
            public int Sales { get; }
            public int SatisfactionDelta { get; }

            public InnEconomyStatistics(
                int guests,
                int rejectedGuests,
                int sales,
                int satisfactionDelta)
            {
                Guests = guests;
                RejectedGuests = rejectedGuests;
                Sales = sales;
                SatisfactionDelta = satisfactionDelta;
            }
        }
    }
}
