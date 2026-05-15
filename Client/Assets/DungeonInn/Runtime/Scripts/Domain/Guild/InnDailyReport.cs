namespace DungeonInn.Domain.Guild
{
    public readonly struct InnDailyReport
    {
        public int Day { get; }
        public InnEconomySummary Summary { get; }
        public int Guests => Summary.Guests;
        public int RejectedGuests => Summary.RejectedGuests;
        public int Demand => Summary.Demand;
        public int Sales => Summary.Sales;
        public int SatisfactionDelta => Summary.SatisfactionDelta;
        public int Reputation => Summary.Reputation;
        public int OccupiedRooms => Summary.OccupiedRooms;
        public int RoomCapacity => Summary.RoomCapacity;
        public int OccupancyPercent => Summary.OccupancyPercent;
        public int GuildGold => Summary.GuildGold;
        public int RookieSwordStock => Summary.RookieSwordStock;
        public int RookieArmorStock => Summary.RookieArmorStock;

        public InnDailyReport(
            int day,
            InnEconomySummary summary)
        {
            Day = day;
            Summary = summary;
        }
    }
}
