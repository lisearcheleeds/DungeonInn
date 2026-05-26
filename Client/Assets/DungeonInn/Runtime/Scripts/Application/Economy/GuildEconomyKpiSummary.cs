namespace DungeonInn.Application.Economy
{
    public readonly struct GuildEconomyKpiSummary
    {
        public GuildEconomyKpiSummary(
            int gold,
            int sales,
            int guests,
            int occupancyPercent,
            int reputation)
        {
            Gold = gold;
            Sales = sales;
            Guests = guests;
            OccupancyPercent = occupancyPercent;
            Reputation = reputation;
        }

        public int Gold { get; }
        public int Sales { get; }
        public int Guests { get; }
        public int OccupancyPercent { get; }
        public int Reputation { get; }
    }
}
