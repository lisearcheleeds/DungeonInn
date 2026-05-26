using System;
using DungeonInn.Domain.Facility;

namespace DungeonInn.Application.Economy
{
    public sealed class GuildInventorySourceSummary
    {
        public GuildInventorySourceSummary(
            GuildInventorySourceType sourceType,
            Guid sourceId,
            string sourceName,
            FacilityType? facilityType,
            int count)
        {
            if (string.IsNullOrWhiteSpace(sourceName))
            {
                throw new ArgumentException("Source name is required.", nameof(sourceName));
            }

            SourceType = sourceType;
            SourceId = sourceId;
            SourceName = sourceName;
            FacilityType = facilityType;
            Count = Math.Max(0, count);
        }

        public GuildInventorySourceType SourceType { get; }
        public Guid SourceId { get; }
        public string SourceName { get; }
        public FacilityType? FacilityType { get; }
        public int Count { get; }
    }
}
