using System;

namespace DungeonInn.Application.Dungeons
{
    public sealed class DungeonLayerItemDropSummary
    {
        public DungeonLayerItemDropSummary(
            int itemId,
            string itemName,
            string sourceMonsterName,
            float dropChance,
            int minCount,
            int maxCount)
        {
            if (itemId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(itemId));
            }

            if (string.IsNullOrWhiteSpace(itemName))
            {
                throw new ArgumentException("Item name is required.", nameof(itemName));
            }

            if (string.IsNullOrWhiteSpace(sourceMonsterName))
            {
                throw new ArgumentException("Source monster name is required.", nameof(sourceMonsterName));
            }

            ItemId = itemId;
            ItemName = itemName;
            SourceMonsterName = sourceMonsterName;
            DropChance = Math.Clamp(dropChance, 0f, 1f);
            MinCount = Math.Max(1, minCount);
            MaxCount = Math.Max(MinCount, maxCount);
        }

        public int ItemId { get; }
        public string ItemName { get; }
        public string SourceMonsterName { get; }
        public float DropChance { get; }
        public int MinCount { get; }
        public int MaxCount { get; }
    }
}
