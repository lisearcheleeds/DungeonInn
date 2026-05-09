using System.Collections.Generic;
using DungeonInn.Domain.Item;

namespace DungeonInn.Master
{
    public interface IMasterRepository : IItemMasterRepository, IItemStackLimitResolver
    {
        IReadOnlyDictionary<WeaponType, WeaponTypeCombatMaster> WeaponTypeCombatMasters { get; }
        IReadOnlyDictionary<int, ActorArchetypeMaster> ActorArchetypeMasters { get; }
        IReadOnlyDictionary<int, AdventurerSpawnMaster> AdventurerSpawnMasters { get; }
        IReadOnlyDictionary<int, SpeciesMaster> SpeciesMasters { get; }
        IReadOnlyDictionary<int, SpawnTableMaster> SpawnTableMasters { get; }
        IReadOnlyDictionary<int, LevelTable> LevelTables { get; }
        IReadOnlyDictionary<int, DungeonFloorExplorationMaster> DungeonFloorExplorationMasters { get; }

        WeaponTypeCombatMaster GetWeaponTypeCombatMaster(WeaponType weaponType);
        ActorArchetypeMaster GetActorArchetypeMaster(int archetypeId);
        AdventurerSpawnMaster GetAdventurerSpawnMaster(int adventurerSpawnId);
        SpeciesMaster GetSpeciesMaster(int speciesId);
        SpawnTableMaster GetSpawnTableMaster(int spawnTableId);
        LevelTable GetLevelTable(int levelTableId);
        DungeonFloorExplorationMaster GetDungeonFloorExplorationMaster(int floorIndex);
    }
}
