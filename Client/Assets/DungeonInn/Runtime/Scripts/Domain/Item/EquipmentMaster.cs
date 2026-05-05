using System;

namespace DungeonInn.Domain.Item
{
    public sealed class EquipmentMaster
    {
        public int ItemId { get; }
        public EquipmentSlot Slot { get; }
        public WeaponType WeaponType { get; }
        public int Attack { get; }
        public int Defense { get; }
        public int Modifier { get; }
        public int RecommendedLevel { get; }

        public EquipmentMaster(
            int itemId,
            EquipmentSlot slot,
            int attack,
            int defense,
            int modifier,
            int recommendedLevel)
            : this(itemId, slot, WeaponType.None, attack, defense, modifier, recommendedLevel)
        {
        }

        public EquipmentMaster(
            int itemId,
            EquipmentSlot slot,
            WeaponType weaponType,
            int attack,
            int defense,
            int modifier,
            int recommendedLevel)
        {
            ItemId = itemId;
            Slot = slot;
            if (slot == EquipmentSlot.Weapon && weaponType == WeaponType.None)
            {
                throw new ArgumentException("Weapon type is required for weapon equipment.", nameof(weaponType));
            }

            WeaponType = slot == EquipmentSlot.Weapon ? weaponType : WeaponType.None;
            Attack = Math.Max(0, attack);
            Defense = Math.Max(0, defense);
            Modifier = modifier;
            RecommendedLevel = Math.Max(1, recommendedLevel);
        }
    }
}
