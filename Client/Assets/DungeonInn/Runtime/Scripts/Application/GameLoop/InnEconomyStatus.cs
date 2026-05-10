namespace DungeonInn.Application.GameLoop
{
    public readonly struct InnEconomyStatus
    {
        public int CurrentDay { get; }
        public int GuestsToday { get; }
        public int RejectedGuestsToday { get; }
        public int DemandToday { get; }
        public int SalesToday { get; }
        public int SatisfactionDeltaToday { get; }
        public int Reputation { get; }
        public int OccupiedRooms { get; }
        public int RoomCapacity { get; }
        public int OccupancyPercent { get; }
        public int GuildGold { get; }
        public int RookieSwordStock { get; }
        public int RookieArmorStock { get; }

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
            GuestsToday = guestsToday;
            RejectedGuestsToday = rejectedGuestsToday;
            DemandToday = demandToday;
            SalesToday = salesToday;
            SatisfactionDeltaToday = satisfactionDeltaToday;
            Reputation = reputation;
            OccupiedRooms = occupiedRooms;
            RoomCapacity = roomCapacity;
            OccupancyPercent = occupancyPercent;
            GuildGold = guildGold;
            RookieSwordStock = rookieSwordStock;
            RookieArmorStock = rookieArmorStock;
        }
    }
}
