using System.Collections.Generic;
using DungeonInn.Domain.Item;
using DungeonInn.Master;
using DungeonInn.Application.World;

namespace DungeonInn.Tests.EditMode
{
    public static class ActorViewDataStoreTestFactory
    {
        public static ActorViewDataStore Create()
        {
            return new ActorViewDataStore(new TestMasterRepository());
        }

        sealed class TestMasterRepository : IMasterRepository
        {
            readonly HardcodedMasterRepository inner = new();
            readonly ActorArchetypeMaster fallbackArchetype;

            public TestMasterRepository()
            {
                var source = inner.GetActorArchetypeMaster(1);
                fallbackArchetype = new ActorArchetypeMaster(
                    1,
                    source.Name,
                    source.VisualId,
                    source.BehaviorType,
                    source.SpeciesId,
                    source.DefaultWeaponType,
                    source.BaseStats,
                    source.InitialLevel,
                    source.LevelTableId);
            }

            public IReadOnlyDictionary<int, ItemMaster> ItemMasters => inner.ItemMasters;
            public IReadOnlyDictionary<int, EquipmentMaster> EquipmentMasters => inner.EquipmentMasters;
            public IReadOnlyDictionary<int, WeaponMaster> WeaponMasters => inner.WeaponMasters;
            public IReadOnlyDictionary<WeaponType, WeaponTypeCombatMaster> WeaponTypeCombatMasters => inner.WeaponTypeCombatMasters;
            public IReadOnlyDictionary<int, ActorArchetypeMaster> ActorArchetypeMasters => inner.ActorArchetypeMasters;
            public IReadOnlyDictionary<int, AdventurerSpawnMaster> AdventurerSpawnMasters => inner.AdventurerSpawnMasters;
            public IReadOnlyDictionary<int, ActorEffectMaster> ActorEffectMasters => inner.ActorEffectMasters;
            public IReadOnlyDictionary<int, SpeciesMaster> SpeciesMasters => inner.SpeciesMasters;
            public IReadOnlyDictionary<int, SpawnTableMaster> SpawnTableMasters => inner.SpawnTableMasters;
            public IReadOnlyDictionary<int, LevelTable> LevelTables => inner.LevelTables;
            public IReadOnlyDictionary<int, DungeonFloorExplorationMaster> DungeonFloorExplorationMasters => inner.DungeonFloorExplorationMasters;
            public IReadOnlyDictionary<string, EnvironmentPropVisualMaster> EnvironmentPropVisualMasters => inner.EnvironmentPropVisualMasters;
            public IReadOnlyDictionary<string, ActorVisualMaster> ActorVisualMasters => inner.ActorVisualMasters;

            public ItemMaster GetItemMaster(int itemId)
            {
                return inner.GetItemMaster(itemId);
            }

            public EquipmentMaster GetEquipmentMaster(int itemId)
            {
                return inner.GetEquipmentMaster(itemId);
            }

            public WeaponMaster GetWeaponMaster(int itemId)
            {
                return inner.GetWeaponMaster(itemId);
            }

            public WeaponTypeCombatMaster GetWeaponTypeCombatMaster(WeaponType weaponType)
            {
                return inner.GetWeaponTypeCombatMaster(weaponType);
            }

            public ActorArchetypeMaster GetActorArchetypeMaster(int archetypeId)
            {
                if (archetypeId == 0)
                {
                    return fallbackArchetype;
                }

                return inner.GetActorArchetypeMaster(archetypeId);
            }

            public AdventurerSpawnMaster GetAdventurerSpawnMaster(int adventurerSpawnId)
            {
                return inner.GetAdventurerSpawnMaster(adventurerSpawnId);
            }

            public ActorEffectMaster GetActorEffectMaster(int actorEffectId)
            {
                return inner.GetActorEffectMaster(actorEffectId);
            }

            public SpeciesMaster GetSpeciesMaster(int speciesId)
            {
                return inner.GetSpeciesMaster(speciesId);
            }

            public SpawnTableMaster GetSpawnTableMaster(int spawnTableId)
            {
                return inner.GetSpawnTableMaster(spawnTableId);
            }

            public LevelTable GetLevelTable(int levelTableId)
            {
                return inner.GetLevelTable(levelTableId);
            }

            public int GetMaxStackCount(int itemId)
            {
                return inner.GetMaxStackCount(itemId);
            }

            public DungeonFloorExplorationMaster GetDungeonFloorExplorationMaster(int floorIndex)
            {
                return inner.GetDungeonFloorExplorationMaster(floorIndex);
            }

            public EnvironmentPropVisualMaster GetEnvironmentPropVisualMaster(string key)
            {
                return inner.GetEnvironmentPropVisualMaster(key);
            }

            public ActorVisualMaster GetActorVisualMaster(string visualId, int skinId)
            {
                return inner.GetActorVisualMaster(visualId, skinId);
            }
        }
    }
}

