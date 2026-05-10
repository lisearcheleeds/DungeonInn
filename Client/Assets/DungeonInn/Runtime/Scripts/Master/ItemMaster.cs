using System;
using DungeonInn.Domain.Item;

namespace DungeonInn.Master
{
    public sealed class ItemMaster
    {
        public int Id { get; }
        public string Name { get; }
        public ItemCategory Category { get; }
        public int BasePrice { get; }
        public int Quality { get; }
        public bool CanTrade { get; }
        public int MaxStackCount { get; }
        public int ActorEffectMasterId { get; }

        public ItemMaster(
            int id,
            string name,
            ItemCategory category,
            int basePrice,
            int quality,
            bool canTrade,
            int maxStackCount = int.MaxValue,
            int actorEffectMasterId = 0)
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

            if (maxStackCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxStackCount));
            }

            if (actorEffectMasterId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(actorEffectMasterId));
            }

            Id = id;
            Name = name;
            Category = category;
            BasePrice = basePrice;
            Quality = Math.Max(0, quality);
            CanTrade = canTrade;
            MaxStackCount = maxStackCount;
            ActorEffectMasterId = actorEffectMasterId;
        }
    }
}
