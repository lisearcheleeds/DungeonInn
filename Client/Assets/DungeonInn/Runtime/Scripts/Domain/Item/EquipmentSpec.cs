using System;

namespace DungeonInn.Domain.Item
{
    public sealed class EquipmentSpec
    {
        public int ItemId { get; }
        public EquipmentSlot Slot { get; }
        public int Attack { get; }
        public int Defense { get; }
        public int Modifier { get; }
        public int RecommendedLevel { get; }

        public EquipmentSpec(
            int itemId,
            EquipmentSlot slot,
            int attack,
            int defense,
            int modifier,
            int recommendedLevel)
        {
            ItemId = itemId;
            Slot = slot;
            Attack = Math.Max(0, attack);
            Defense = Math.Max(0, defense);
            Modifier = modifier;
            RecommendedLevel = Math.Max(1, recommendedLevel);
        }
    }
}
