using System;
using System.Collections.Generic;
using DungeonInn.Domain.Commerce;
using DungeonInn.Domain.Item;

namespace DungeonInn.Domain.Facility
{
    public sealed class Facility : IExchangeParticipant
    {
        const int PointsPerLevel = 100;

        public Guid Id { get; }
        public FacilityType Type { get; }
        public string Name { get; }
        public int BasePrice { get; }
        public int StaffPoint { get; private set; }
        public int Level { get; private set; }
        public int Quality { get; private set; }
        public int Capacity { get; private set; }
        public Inventory Inventory { get; }

        public Facility(
            Guid id,
            FacilityType type,
            string name,
            int basePrice,
            int capacity,
            Inventory inventory)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Facility name is required.", nameof(name));
            }

            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            if (basePrice < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(basePrice));
            }

            Id = id;
            Type = type;
            Name = name;
            BasePrice = basePrice;
            Capacity = capacity;
            Inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            Level = 1;
            Quality = 1;
        }

        public void ApplyStaffPoint(int staffPoint)
        {
            StaffPoint = Math.Max(0, staffPoint);
            Level = 1 + StaffPoint / PointsPerLevel;
            Quality = Level;
            Capacity = Math.Max(1, Level);
        }

        public ItemStack CalculateUsagePrice(FacilityUsageRequest request)
        {
            var multiplier = request.PurchasedItems.Count == 0 ? 1 : request.PurchasedItems.Count;
            return new ItemStack(SpecialItemIds.Money, BasePrice * multiplier * Level);
        }

        bool IExchangeParticipant.HasAll(IReadOnlyList<ItemStack> items)
        {
            return Inventory.HasAll(items);
        }

        bool IExchangeParticipant.CanAddAfterRemoving(IReadOnlyList<ItemStack> toRemove, IReadOnlyList<ItemStack> toAdd)
        {
            return Inventory.CanAddAfterRemoving(toRemove, toAdd);
        }

        void IExchangeParticipant.RemoveRange(IReadOnlyList<ItemStack> items)
        {
            Inventory.RemoveRange(items);
        }

        void IExchangeParticipant.AddRange(IReadOnlyList<ItemStack> items)
        {
            Inventory.AddRange(items);
        }
    }
}
