namespace DungeonInn.Application.GameLoop
{
    using DungeonInn.Domain.Guild;

    public readonly struct InnEconomyStatus
    {
        public int CurrentDay { get; }
        public InnEconomySummary Current { get; }
        public int GuestsToday => Current.Guests;
        public int RejectedGuestsToday => Current.RejectedGuests;
        public int DemandToday => Current.Demand;
        public int SalesToday => Current.Sales;
        public int SatisfactionDeltaToday => Current.SatisfactionDelta;
        public int Reputation => Current.Reputation;
        public int OccupiedRooms => Current.OccupiedRooms;
        public int RoomCapacity => Current.RoomCapacity;
        public int OccupancyPercent => Current.OccupancyPercent;
        public int GuildGold => Current.GuildGold;
        public int RookieSwordStock => Current.RookieSwordStock;
        public int RookieArmorStock => Current.RookieArmorStock;

        public InnEconomyStatus(
            int currentDay,
            int guestsToday,
            int rejectedGuestsToday,
            int demandToday,
            int salesToday,
            int satisfactionDeltaToday,
            int reputation,
            int occupiedRooms,
            int roomCapacity,
            int occupancyPercent,
            int guildGold,
            int rookieSwordStock,
            int rookieArmorStock)
        {
            CurrentDay = currentDay;
            Current = new InnEconomySummary(
                guestsToday,
                rejectedGuestsToday,
                demandToday,
                salesToday,
                satisfactionDeltaToday,
                reputation,
                occupiedRooms,
                roomCapacity,
                occupancyPercent,
                guildGold,
                rookieSwordStock,
                rookieArmorStock);
        }
    }
}
