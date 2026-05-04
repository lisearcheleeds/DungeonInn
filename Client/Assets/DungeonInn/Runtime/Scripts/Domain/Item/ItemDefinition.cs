using System;
namespace DungeonInn.Domain.Item
{
    public sealed class ItemDefinition
    {
        public int Id { get; }
        public string Name { get; }
        public ItemCategory Category { get; }
        public int BasePrice { get; }
        public int Quality { get; }
        public bool CanTrade { get; }

        public ItemDefinition(
            int id,
            string name,
            ItemCategory category,
            int basePrice,
            int quality,
            bool canTrade)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Item name is required.", nameof(name));
            }

            if (id < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            if (basePrice < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(basePrice));
            }

            if (canTrade && basePrice < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(basePrice));
            }

            Id = id;
            Name = name;
            Category = category;
            BasePrice = basePrice;
            Quality = Math.Max(0, quality);
            CanTrade = canTrade;
        }
    }
}
