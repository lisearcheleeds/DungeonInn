namespace DungeonInn.Application.Event.Events
{
    public sealed class GuildSupplyReplenished : IGameEvent
    {
        public int ItemId { get; }
        public int Count { get; }
        public int Cost { get; }
        public int RemainingGold { get; }

        public GuildSupplyReplenished(int itemId, int count, int cost, int remainingGold)
        {
            ItemId = itemId;
            Count = count;
            Cost = cost;
            RemainingGold = remainingGold;
        }
    }
}
