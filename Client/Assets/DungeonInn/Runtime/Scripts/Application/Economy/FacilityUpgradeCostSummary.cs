using System;

namespace DungeonInn.Application.Economy
{
    public sealed class FacilityUpgradeCostSummary
    {
        public FacilityUpgradeCostSummary(int itemId, string itemName, int requiredCount, int ownedCount)
        {
            if (itemId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(itemId));
            }

            ItemId = itemId;
            ItemName = string.IsNullOrWhiteSpace(itemName)
                ? throw new ArgumentException("Item name is required.", nameof(itemName))
                : itemName;
            RequiredCount = Math.Max(0, requiredCount);
            OwnedCount = Math.Max(0, ownedCount);
        }

        public int ItemId { get; }
        public string ItemName { get; }
        public int RequiredCount { get; }
        public int OwnedCount { get; }
        public bool IsSatisfied => RequiredCount <= OwnedCount;
    }
}
