using System.Collections.Generic;
using DungeonInn.Domain.Item;

namespace DungeonInn.Master
{
    public interface IMasterRepository : IItemMasterRepository
    {
        IReadOnlyDictionary<WeaponType, WeaponTypeCombatMaster> WeaponTypeCombatMasters { get; }
        IReadOnlyDictionary<int, ActorArchetypeMaster> ActorArchetypeMasters { get; }
        IReadOnlyDictionary<int, MonsterSpeciesMaster> MonsterSpeciesMasters { get; }
        IReadOnlyDictionary<int, SpawnTableMaster> SpawnTableMasters { get; }
        IReadOnlyDictionary<int, LevelTable> LevelTables { get; }

        WeaponTypeCombatMaster GetWeaponTypeCombatMaster(WeaponType weaponType);
        ActorArchetypeMaster GetActorArchetypeMaster(int archetypeId);
        MonsterSpeciesMaster GetMonsterSpeciesMaster(int speciesId);
        SpawnTableMaster GetSpawnTableMaster(int spawnTableId);
        LevelTable GetLevelTable(int levelTableId);
    }
}
