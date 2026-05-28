using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Domain.Item;

namespace DungeonInn.Master
{
    public sealed class ActorLoadoutMaster
    {
        public int Id { get; }
        public int WeaponItemId { get; }
        public int ArmorItemId { get; }
        public IReadOnlyList<int> AccessoryItemIds { get; }
        public IReadOnlyList<ItemStack> InitialInventory { get; }

        public ActorLoadoutMaster(
            int id,
            int weaponItemId,
            int armorItemId,
            IReadOnlyList<int> accessoryItemIds,
            IReadOnlyList<ItemStack> initialInventory)
        {
            if (id < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            if (weaponItemId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(weaponItemId));
            }

            if (armorItemId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(armorItemId));
            }

            Id = id;
            WeaponItemId = weaponItemId;
            ArmorItemId = armorItemId;
            AccessoryItemIds = (accessoryItemIds ?? Array.Empty<int>()).ToArray();
            InitialInventory = (initialInventory ?? Array.Empty<ItemStack>()).ToArray();
        }
    }
}
