namespace DungeonInn.Application.Economy
{
    using DungeonInn.Domain.Guild;

    public readonly struct InnEconomyStatus
    {
        public int CurrentDay { get; }
        public InnEconomySummary Current { get; }

        public InnEconomyStatus(
            int currentDay,
            InnEconomySummary summary)
        {
            CurrentDay = currentDay;
            Current = summary;
        }
    }
}
