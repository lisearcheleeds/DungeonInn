using System;
using System.Collections.Generic;
using DungeonInn.Domain.Commerce;
using DungeonInn.Domain.Item;

namespace DungeonInn.Domain.Facility
{
    public sealed class Facility : IExchangeParticipant
    {
        const int PointsPerLevel = 100;
        readonly Inventory inventory;

        public Guid Id { get; }
        public FacilityType Type { get; }
        public string Name { get; }
        public int BasePrice { get; }
        public int StaffPoint { get; private set; }
        public int Level { get; private set; }
        public int Quality { get; private set; }
        public int Capacity { get; private set; }
        public IReadOnlyInventory Inventory => inventory;

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
            this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            Level = 1;
            Quality = 1;
        }

        public void ApplyStaffPoint(int staffPoint)
        {
            StaffPoint = Math.Max(0, staffPoint);
        }

        public void UpgradeTo(int level, int quality, int capacity)
        {
            if (level <= Level)
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            Level = level;
            Quality = Math.Max(1, quality);
            Capacity = Math.Max(1, capacity);
        }

        public ItemStack CalculateUsagePrice(FacilityUsageRequest request)
        {
            var multiplier = request.PurchasedItems.Count == 0 ? 1 : request.PurchasedItems.Count;
            return new ItemStack(SpecialItemIds.Money, BasePrice * multiplier * Level);
        }

        public void ReceiveUsageFee(int amount)
        {
            inventory.AddGold(amount);
        }

        bool IExchangeParticipant.HasAll(IReadOnlyList<ItemStack> items)
        {
            return inventory.HasAll(items);
        }

        bool IExchangeParticipant.Has(ItemStack item)
        {
            return inventory.Has(item);
        }

        bool IExchangeParticipant.CanAddAfterRemoving(ItemStack toRemove, ItemStack toAdd)
        {
            return inventory.CanAddAfterRemoving(toRemove, toAdd);
        }

        bool IExchangeParticipant.CanAddAfterRemoving(IReadOnlyList<ItemStack> toRemove, IReadOnlyList<ItemStack> toAdd)
        {
            return inventory.CanAddAfterRemoving(toRemove, toAdd);
        }

        void IExchangeParticipant.Remove(ItemStack item)
        {
            inventory.Remove(item);
        }

        void IExchangeParticipant.RemoveRange(IReadOnlyList<ItemStack> items)
        {
            inventory.RemoveRange(items);
        }

        void IExchangeParticipant.Add(ItemStack item)
        {
            inventory.Add(item);
        }

        void IExchangeParticipant.AddRange(IReadOnlyList<ItemStack> items)
        {
            inventory.AddRange(items);
        }
    }
}
