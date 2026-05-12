namespace DungeonInn.Application.GameLoop
{
    public readonly struct InnEconomyStatistics
    {
        public int Guests { get; }
        public int RejectedGuests { get; }
        public int Sales { get; }
        public int SatisfactionDelta { get; }
        public int Demand => Guests + RejectedGuests;

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
