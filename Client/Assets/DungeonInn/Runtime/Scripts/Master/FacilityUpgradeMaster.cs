using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Item;

namespace DungeonInn.Master
{
    public sealed class FacilityUpgradeMaster
    {
        public FacilityUpgradeMaster(
            int id,
            FacilityType facilityType,
            int fromLevel,
            int toLevel,
            int quality,
            int capacity,
            IReadOnlyList<ItemStack> costs)
        {
            if (id < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            if (fromLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(fromLevel));
            }

            if (toLevel <= fromLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(toLevel));
            }

            Id = id;
            FacilityType = facilityType;
            FromLevel = fromLevel;
            ToLevel = toLevel;
            Quality = Math.Max(1, quality);
            Capacity = Math.Max(1, capacity);
            Costs = (costs ?? Array.Empty<ItemStack>()).ToArray();
        }

        public int Id { get; }
        public FacilityType FacilityType { get; }
        public int FromLevel { get; }
        public int ToLevel { get; }
        public int Quality { get; }
        public int Capacity { get; }
        public IReadOnlyList<ItemStack> Costs { get; }
    }
}
