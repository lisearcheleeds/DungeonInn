namespace DungeonInn.Domain.Guild
{
    public readonly struct InnDailyReport
    {
        public int Day { get; }
        public int Guests { get; }
        public int RejectedGuests { get; }
        public int Demand { get; }
        public int Sales { get; }
        public int SatisfactionDelta { get; }
        public int Reputation { get; }
        public int OccupiedRooms { get; }
        public int RoomCapacity { get; }
        public int OccupancyPercent { get; }
        public int GuildGold { get; }
        public int RookieSwordStock { get; }
        public int RookieArmorStock { get; }

        public InnDailyReport(
            int day,
            int guests,
            int rejectedGuests,
            int demand,
            int sales,
            int satisfactionDelta,
            int reputation,
            int occupiedRooms,
            int roomCapacity,
            int occupancyPercent,
            int guildGold,
            int rookieSwordStock,
            int rookieArmorStock)
        {
            Day = day;
            Guests = guests;
            RejectedGuests = rejectedGuests;
            Demand = demand;
            Sales = sales;
            SatisfactionDelta = satisfactionDelta;
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
