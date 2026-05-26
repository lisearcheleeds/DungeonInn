using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Domain.Item;

namespace DungeonInn.Application.Economy
{
    public sealed class GuildCombinedInventoryItemSummary
    {
        public GuildCombinedInventoryItemSummary(
            int itemId,
            string itemName,
            ItemTag tags,
            int totalCount,
            IReadOnlyList<GuildInventorySourceSummary> sources)
        {
            if (itemId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(itemId));
            }

            if (string.IsNullOrWhiteSpace(itemName))
            {
                throw new ArgumentException("Item name is required.", nameof(itemName));
            }

            ItemId = itemId;
            ItemName = itemName;
            Tags = tags;
            TotalCount = Math.Max(0, totalCount);
            Sources = (sources ?? Array.Empty<GuildInventorySourceSummary>()).ToArray();
        }

        public int ItemId { get; }
        public string ItemName { get; }
        public ItemTag Tags { get; }
        public int TotalCount { get; }
        public IReadOnlyList<GuildInventorySourceSummary> Sources { get; }
    }
}
