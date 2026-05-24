using System;
using System.Collections.Generic;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Item;

namespace DungeonInn.Application.World
{
    public sealed class InitialWorldSettings
    {
        public int DungeonSeed { get; }
        public int GameRandomSeed { get; }
        public int GuildGold { get; }
        public int GeneralStoreGold { get; }
        public int EquipmentShopGold { get; }
        public int InnReputation { get; }
        public int GuildInventorySlotCapacity { get; }
        public int InnBasePrice { get; }
        public int InnCapacity { get; }
        public int GeneralStoreBasePrice { get; }
        public int EquipmentShopBasePrice { get; }
        public int ShopCapacity { get; }
        public int InitialRookieSwordItemId { get; }
        public int InitialRookieArmorItemId { get; }
        public IReadOnlyList<ItemStack> InitialGuildInventory { get; }

        public int GuildReserveGold => GuildGold - GeneralStoreGold - EquipmentShopGold;

        public InitialWorldSettings(
            int dungeonSeed,
            int gameRandomSeed,
            int guildGold,
            int generalStoreGold,
            int equipmentShopGold,
            int innReputation,
            int guildInventorySlotCapacity,
            int innBasePrice,
            int innCapacity,
            int generalStoreBasePrice,
            int equipmentShopBasePrice,
            int shopCapacity,
            int initialRookieSwordItemId,
            int initialRookieArmorItemId,
            IReadOnlyList<ItemStack> initialGuildInventory)
        {
            DungeonSeed = dungeonSeed;
            GameRandomSeed = gameRandomSeed;
            GuildGold = Math.Max(0, guildGold);
            GeneralStoreGold = Math.Max(0, generalStoreGold);
            EquipmentShopGold = Math.Max(0, equipmentShopGold);
            InnReputation = Math.Max(0, innReputation);
            GuildInventorySlotCapacity = Math.Max(1, guildInventorySlotCapacity);
            InnBasePrice = Math.Max(0, innBasePrice);
            InnCapacity = Math.Max(1, innCapacity);
            GeneralStoreBasePrice = Math.Max(0, generalStoreBasePrice);
            EquipmentShopBasePrice = Math.Max(0, equipmentShopBasePrice);
            ShopCapacity = Math.Max(1, shopCapacity);
            InitialRookieSwordItemId = Math.Max(1, initialRookieSwordItemId);
            InitialRookieArmorItemId = Math.Max(1, initialRookieArmorItemId);
            InitialGuildInventory = initialGuildInventory ?? throw new ArgumentNullException(nameof(initialGuildInventory));
        }

        public static InitialWorldSettings CreateDefault()
        {
            return new InitialWorldSettings(
                12345,
                42195,
                10000,
                1000,
                1000,
                10,
                100,
                10,
                8,
                10,
                10,
                1,
                3001,
                3003,
                new[]
                {
                    new ItemStack(SpecialItemIds.Money, 8000),
                    new ItemStack(3001, 20),
                    new ItemStack(3003, 20)
                });
        }
    }
}
