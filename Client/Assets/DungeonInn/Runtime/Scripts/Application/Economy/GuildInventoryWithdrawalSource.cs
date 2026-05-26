using System;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Item;

namespace DungeonInn.Application.Economy
{
    public sealed class GuildInventoryWithdrawalSource
    {
        public GuildInventoryWithdrawalSource(
            Guid sourceId,
            string sourceName,
            FacilityType? facilityType,
            ItemStack itemStack)
        {
            SourceId = sourceId;
            SourceName = sourceName ?? string.Empty;
            FacilityType = facilityType;
            ItemStack = itemStack;
        }

        public Guid SourceId { get; }
        public string SourceName { get; }
        public FacilityType? FacilityType { get; }
        public ItemStack ItemStack { get; }
    }
}
