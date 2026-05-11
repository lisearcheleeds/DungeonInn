using DungeonInn.Domain.Common;
using DungeonInn.Domain.Facility;

namespace DungeonInn.Application.GameLoop
{
    public sealed class InnEconomyStatusCalculator
    {
        public InnEconomyStatus Calculate(IGameWorldStateReader worldState, int currentDay)
        {
            var economy = worldState.InnEconomy;
            var roomCapacity = CountRoomCapacity(worldState);
            var occupiedRooms = CountOccupiedRooms(worldState);
            var occupancyPercent = roomCapacity <= 0 ? 0 : occupiedRooms * 100 / roomCapacity;

            return new InnEconomyStatus(
                currentDay,
                economy.TodayGuests,
                economy.TodayRejectedGuests,
                economy.TodayGuests + economy.TodayRejectedGuests,
                economy.TodaySales,
                economy.TodaySatisfactionDelta,
                economy.Reputation,
                occupiedRooms,
                roomCapacity,
                occupancyPercent,
                worldState.Guild.Inventory.Gold,
                CountItem(worldState, GameConstants.InitialRookieSwordItemId),
                CountItem(worldState, GameConstants.InitialRookieArmorItemId));
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
    }
}
