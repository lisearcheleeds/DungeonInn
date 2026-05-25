using System;

namespace DungeonInn.Domain.Item
{
    [Flags]
    public enum ItemTag
    {
        None = 0,
        Currency = 1 << 0,
        Recovery = 1 << 1,
        ManaRecovery = 1 << 2,
        Material = 1 << 3,
        Weapon = 1 << 4,
        Armor = 1 << 5,
        Accessory = 1 << 6,
        Valuable = 1 << 7,
        SellOnly = 1 << 8,
        Food = 1 << 9,
        Drink = 1 << 10,
        Tool = 1 << 11,
        CraftingComponent = 1 << 12,
        Ore = 1 << 13,
        Metal = 1 << 14,
        Gem = 1 << 15
    }
}
