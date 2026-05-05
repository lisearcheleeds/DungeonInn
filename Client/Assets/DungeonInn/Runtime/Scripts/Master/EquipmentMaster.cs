using System;
using DungeonInn.Domain.Item;

namespace DungeonInn.Master
{
    public sealed class EquipmentMaster
    {
        public int ItemId { get; }
        public EquipmentSlot Slot { get; }
        public int Defense { get; }

        public EquipmentMaster(
            int itemId,
            EquipmentSlot slot,
            int defense)
        {
            if (itemId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(itemId));
            }

            if (slot == EquipmentSlot.None)
            {
                throw new ArgumentException("Equipment slot is required.", nameof(slot));
            }

            ItemId = itemId;
            Slot = slot;
            Defense = Math.Max(0, defense);
        }
    }
}
