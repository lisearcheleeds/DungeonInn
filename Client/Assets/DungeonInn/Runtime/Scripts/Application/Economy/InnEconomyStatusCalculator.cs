using DungeonInn.Application.World;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Guild;

namespace DungeonInn.Application.Economy
{
    public sealed class InnEconomyStatusCalculator
    {
        public InnEconomyStatus Calculate(
            IGameWorldStateReader worldState,
            int currentDay,
            InnEconomyStatistics statistics)
        {
            var report = CalculateDailyReport(
                worldState,
                currentDay,
                statistics);

            return new InnEconomyStatus(report.Day, report.Summary);
        }

        public InnDailyReport CalculateDailyReport(
            IGameWorldStateReader worldState,
            int day,
            InnEconomyStatistics statistics)
        {
            var economy = worldState.InnEconomy;
            var roomCapacity = CountRoomCapacity(worldState);
            var occupiedRooms = CountOccupiedRooms(worldState);
            var occupancyPercent = roomCapacity <= 0 ? 0 : occupiedRooms * 100 / roomCapacity;

            return new InnDailyReport(
                day,
                new InnEconomySummary(
                    statistics.Guests,
                    statistics.RejectedGuests,
                    statistics.Demand,
                    statistics.Sales,
                    statistics.SatisfactionDelta,
                    economy.Reputation,
                    occupiedRooms,
                    roomCapacity,
                    occupancyPercent,
                    CountGold(worldState),
                    CountItem(worldState, GameConstants.InitialRookieSwordItemId),
                    CountItem(worldState, GameConstants.InitialRookieArmorItemId)));
        }

        static int CountItem(IGameWorldStateReader worldState, int itemId)
        {
            var total = worldState.Guild.Inventory.ItemCounts.TryGetValue(itemId, out var guildCount) ? guildCount : 0;
            foreach (var facility in worldState.Guild.Facilities)
            {
                if (facility.Inventory.ItemCounts.TryGetValue(itemId, out var facilityCount))
                {
                    total += facilityCount;
                }
            }

            return total;
        }

        static int CountGold(IGameWorldStateReader worldState)
        {
            var total = worldState.Guild.Inventory.Gold;
            foreach (var facility in worldState.Guild.Facilities)
            {
                total += facility.Inventory.Gold;
            }

            return total;
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

    }
}
