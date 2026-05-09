using System.Collections.Generic;

namespace DungeonInn.Master
{
    public interface IItemMasterRepository
    {
        IReadOnlyDictionary<int, ItemMaster> ItemMasters { get; }
        IReadOnlyDictionary<int, EquipmentMaster> EquipmentMasters { get; }
        IReadOnlyDictionary<int, WeaponMaster> WeaponMasters { get; }

        ItemMaster GetItemMaster(int itemId);
        EquipmentMaster GetEquipmentMaster(int itemId);
        WeaponMaster GetWeaponMaster(int itemId);
    }
}
