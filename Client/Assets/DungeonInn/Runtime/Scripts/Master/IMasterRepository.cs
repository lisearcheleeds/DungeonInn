using System.Collections.Generic;
using DungeonInn.Domain.Item;

namespace DungeonInn.Master
{
    public interface IMasterRepository
    {
        IReadOnlyDictionary<int, ItemMaster> ItemMasters { get; }
        IReadOnlyDictionary<int, EquipmentMaster> EquipmentMasters { get; }
        IReadOnlyDictionary<int, WeaponMaster> WeaponMasters { get; }
        IReadOnlyDictionary<WeaponType, WeaponTypeCombatMaster> WeaponTypeCombatMasters { get; }
        IReadOnlyDictionary<int, ActorArchetypeMaster> ActorArchetypeMasters { get; }
        IReadOnlyDictionary<int, MonsterSpeciesMaster> MonsterSpeciesMasters { get; }
        IReadOnlyDictionary<int, SpawnTableMaster> SpawnTableMasters { get; }

        ItemMaster GetItemMaster(int itemId);
        EquipmentMaster GetEquipmentMaster(int itemId);
        WeaponMaster GetWeaponMaster(int itemId);
        WeaponTypeCombatMaster GetWeaponTypeCombatMaster(WeaponType weaponType);
        ActorArchetypeMaster GetActorArchetypeMaster(int archetypeId);
        MonsterSpeciesMaster GetMonsterSpeciesMaster(int speciesId);
        SpawnTableMaster GetSpawnTableMaster(int spawnTableId);
    }
}
