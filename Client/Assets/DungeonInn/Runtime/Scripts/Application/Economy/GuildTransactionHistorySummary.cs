namespace DungeonInn.Application.Economy
{
    public sealed class GuildTransactionHistorySummary
    {
        public GuildTransactionHistorySummary(int occurredAtTick, string description)
        {
            OccurredAtTick = occurredAtTick;
            Description = description ?? string.Empty;
        }

        public int OccurredAtTick { get; }
        public string Description { get; }
    }
}
