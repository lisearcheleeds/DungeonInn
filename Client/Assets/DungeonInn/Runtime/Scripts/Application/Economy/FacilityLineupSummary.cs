namespace DungeonInn.Application.Economy
{
    public sealed class FacilityLineupSummary
    {
        public FacilityLineupSummary(int itemId, string itemName, int requiredLevel)
        {
            ItemId = itemId;
            ItemName = string.IsNullOrWhiteSpace(itemName) ? $"Item {itemId}" : itemName;
            RequiredLevel = requiredLevel < 1 ? 1 : requiredLevel;
        }

        public int ItemId { get; }
        public string ItemName { get; }
        public int RequiredLevel { get; }
    }
}
