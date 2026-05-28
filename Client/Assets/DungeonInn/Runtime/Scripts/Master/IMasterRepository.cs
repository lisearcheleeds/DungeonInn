using System.Collections.Generic;
using DungeonInn.Domain.Item;

namespace DungeonInn.Master
{
    public interface IMasterRepository : IItemMasterRepository, IItemStackLimitResolver
    {
        IReadOnlyDictionary<WeaponType, WeaponTypeCombatMaster> WeaponTypeCombatMasters { get; }
        IReadOnlyDictionary<int, ActorArchetypeMaster> ActorArchetypeMasters { get; }
        IReadOnlyDictionary<int, ActorLoadoutMaster> ActorLoadoutMasters { get; }
        IReadOnlyDictionary<int, AdventurerSpawnMaster> AdventurerSpawnMasters { get; }
        IReadOnlyDictionary<int, AdventurerSpawnBandMaster> AdventurerSpawnBandMasters { get; }
        IReadOnlyDictionary<int, ActorEffectMaster> ActorEffectMasters { get; }
        IReadOnlyDictionary<int, SpeciesMaster> SpeciesMasters { get; }
        IReadOnlyDictionary<int, SpawnTableMaster> SpawnTableMasters { get; }
        IReadOnlyDictionary<int, LevelTable> LevelTables { get; }
        IReadOnlyDictionary<int, DungeonDepthBandMaster> DungeonDepthBandMasters { get; }
        IReadOnlyDictionary<string, EnvironmentPropVisualMaster> EnvironmentPropVisualMasters { get; }
        IReadOnlyDictionary<string, ActorVisualMaster> ActorVisualMasters { get; }

        WeaponTypeCombatMaster GetWeaponTypeCombatMaster(WeaponType weaponType);
        ActorArchetypeMaster GetActorArchetypeMaster(int archetypeId);
        ActorLoadoutMaster GetActorLoadoutMaster(int loadoutId);
        AdventurerSpawnMaster GetAdventurerSpawnMaster(int adventurerSpawnId);
        AdventurerSpawnBandMaster GetAdventurerSpawnBandMaster(int currentDay);
        ActorEffectMaster GetActorEffectMaster(int actorEffectId);
        SpeciesMaster GetSpeciesMaster(int speciesId);
        SpawnTableMaster GetSpawnTableMaster(int spawnTableId);
        LevelTable GetLevelTable(int levelTableId);
        DungeonDepthBandMaster GetDungeonDepthBandMasterForFloor(int floorIndex);
        EnvironmentPropVisualMaster GetEnvironmentPropVisualMaster(string key);
        ActorVisualMaster GetActorVisualMaster(string visualId, int skinId);
    }
}
