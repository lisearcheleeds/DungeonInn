using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Item;

namespace DungeonInn.Master
{
    public sealed class HardcodedMasterRepository : IMasterRepository, IItemStackLimitResolver
    {
        readonly IReadOnlyDictionary<int, ItemMaster> itemMasters;
        readonly IReadOnlyDictionary<int, EquipmentMaster> equipmentMasters;
        readonly IReadOnlyDictionary<int, WeaponMaster> weaponMasters;
        readonly IReadOnlyDictionary<WeaponType, WeaponTypeCombatMaster> weaponTypeCombatMasters;
        readonly IReadOnlyDictionary<int, ActorArchetypeMaster> actorArchetypeMasters;
        readonly IReadOnlyDictionary<int, AdventurerSpawnMaster> adventurerSpawnMasters;
        readonly IReadOnlyDictionary<int, SpeciesMaster> speciesMasters;
        readonly IReadOnlyDictionary<int, SpawnTableMaster> spawnTableMasters;
        readonly IReadOnlyDictionary<int, LevelTable> levelTables;
        readonly IReadOnlyDictionary<int, DungeonFloorExplorationMaster> dungeonFloorExplorationMasters;

        public IReadOnlyDictionary<int, ItemMaster> ItemMasters => itemMasters;
        public IReadOnlyDictionary<int, EquipmentMaster> EquipmentMasters => equipmentMasters;
        public IReadOnlyDictionary<int, WeaponMaster> WeaponMasters => weaponMasters;
        public IReadOnlyDictionary<WeaponType, WeaponTypeCombatMaster> WeaponTypeCombatMasters => weaponTypeCombatMasters;
        public IReadOnlyDictionary<int, ActorArchetypeMaster> ActorArchetypeMasters => actorArchetypeMasters;
        public IReadOnlyDictionary<int, AdventurerSpawnMaster> AdventurerSpawnMasters => adventurerSpawnMasters;
        public IReadOnlyDictionary<int, SpeciesMaster> SpeciesMasters => speciesMasters;
        public IReadOnlyDictionary<int, SpawnTableMaster> SpawnTableMasters => spawnTableMasters;
        public IReadOnlyDictionary<int, LevelTable> LevelTables => levelTables;
        public IReadOnlyDictionary<int, DungeonFloorExplorationMaster> DungeonFloorExplorationMasters => dungeonFloorExplorationMasters;

        public HardcodedMasterRepository()
        {
            itemMasters = CreateItemMasters();
            equipmentMasters = CreateEquipmentMasters();
            weaponTypeCombatMasters = WeaponTypeCombatMasterCatalog.CreateAll();
            weaponMasters = CreateWeaponMasters();
            levelTables = CreateLevelTables();
            dungeonFloorExplorationMasters = CreateDungeonFloorExplorationMasters();
            speciesMasters = CreateSpeciesMasters();
            actorArchetypeMasters = CreateActorArchetypeMasters();
            adventurerSpawnMasters = CreateAdventurerSpawnMasters();
            spawnTableMasters = CreateSpawnTableMasters();
            ValidateReferences();
        }

        public ItemMaster GetItemMaster(int itemId)
        {
            return GetRequired(itemMasters, itemId, nameof(ItemMaster));
        }

        public EquipmentMaster GetEquipmentMaster(int itemId)
        {
            return GetRequired(equipmentMasters, itemId, nameof(EquipmentMaster));
        }

        public WeaponMaster GetWeaponMaster(int itemId)
        {
            return GetRequired(weaponMasters, itemId, nameof(WeaponMaster));
        }

        public WeaponTypeCombatMaster GetWeaponTypeCombatMaster(WeaponType weaponType)
        {
            return GetRequired(weaponTypeCombatMasters, weaponType, nameof(WeaponTypeCombatMaster));
        }

        public ActorArchetypeMaster GetActorArchetypeMaster(int archetypeId)
        {
            return GetRequired(actorArchetypeMasters, archetypeId, nameof(ActorArchetypeMaster));
        }

        public AdventurerSpawnMaster GetAdventurerSpawnMaster(int adventurerSpawnId)
        {
            return GetRequired(adventurerSpawnMasters, adventurerSpawnId, nameof(AdventurerSpawnMaster));
        }

        public SpeciesMaster GetSpeciesMaster(int speciesId)
        {
            return GetRequired(speciesMasters, speciesId, nameof(SpeciesMaster));
        }

        public SpawnTableMaster GetSpawnTableMaster(int spawnTableId)
        {
            return GetRequired(spawnTableMasters, spawnTableId, nameof(SpawnTableMaster));
        }

        public LevelTable GetLevelTable(int levelTableId)
        {
            return GetRequired(levelTables, levelTableId, nameof(LevelTable));
        }

        public int GetMaxStackCount(int itemId)
        {
            return GetItemMaster(itemId).MaxStackCount;
        }

        public DungeonFloorExplorationMaster GetDungeonFloorExplorationMaster(int floorIndex)
        {
            return GetRequired(dungeonFloorExplorationMasters, floorIndex, nameof(DungeonFloorExplorationMaster));
        }

        static IReadOnlyDictionary<int, ItemMaster> CreateItemMasters()
        {
            return new[]
            {
                new ItemMaster(1, "Gold", ItemCategory.Material, 1, 1, false, 100000),
                new ItemMaster(1001, "Herb", ItemCategory.Material, 10, 1, true, 10),
                new ItemMaster(1002, "Goblin Ear", ItemCategory.Material, 25, 1, true, 10),
                new ItemMaster(2001, "Potion", ItemCategory.Consumable, 30, 1, true, 10),
                new ItemMaster(3001, "Novice Sword", ItemCategory.Equipment, 80, 1, true, 1),
                new ItemMaster(3002, "Novice Bow", ItemCategory.Equipment, 80, 1, true, 1),
                new ItemMaster(3003, "Cloth Armor", ItemCategory.Equipment, 60, 1, true, 1),
                new ItemMaster(3004, "Iron Sword", ItemCategory.Equipment, 120, 5, true, 1)
            }.ToDictionary(x => x.Id);
        }

        static IReadOnlyDictionary<int, DungeonFloorExplorationMaster> CreateDungeonFloorExplorationMasters()
        {
            return new[]
            {
                new DungeonFloorExplorationMaster(1, 2, Domain.Common.GameConstants.DungeonFloorDifficultyCoefficient),
                new DungeonFloorExplorationMaster(2, 3, Domain.Common.GameConstants.DungeonFloorDifficultyCoefficient),
                new DungeonFloorExplorationMaster(3, 4, Domain.Common.GameConstants.DungeonFloorDifficultyCoefficient)
            }.ToDictionary(x => x.FloorIndex);
        }

        static IReadOnlyDictionary<int, EquipmentMaster> CreateEquipmentMasters()
        {
            return new[]
            {
                new EquipmentMaster(3001, EquipmentSlot.Weapon, 0, new[]
                {
                    new StatBonus(StatType.Strength, 3),
                    new StatBonus(StatType.Dexterity, 1)
                }),
                new EquipmentMaster(3002, EquipmentSlot.Weapon, 0, new[]
                {
                    new StatBonus(StatType.Dexterity, 3),
                    new StatBonus(StatType.Strength, 1)
                }),
                new EquipmentMaster(3003, EquipmentSlot.Armor, 4, new[]
                {
                    new StatBonus(StatType.Constitution, 2)
                }),
                new EquipmentMaster(3004, EquipmentSlot.Weapon, 0, new[]
                {
                    new StatBonus(StatType.Strength, 5),
                    new StatBonus(StatType.Dexterity, 2)
                })
            }.ToDictionary(x => x.ItemId);
        }

        static IReadOnlyDictionary<int, WeaponMaster> CreateWeaponMasters()
        {
            return new[]
            {
                new WeaponMaster(3001, WeaponType.Sword, 8, 0, 0),
                new WeaponMaster(3002, WeaponType.Bow, 7, 0, 0),
                new WeaponMaster(3004, WeaponType.Sword, 10, 0, 0)
            }.ToDictionary(x => x.ItemId);
        }

        static IReadOnlyDictionary<int, LevelTable> CreateLevelTables()
        {
            var adventurerXp = new int[101];
            var monsterXp = new int[101];
            for (var level = 0; level <= 100; level++)
            {
                adventurerXp[level] = level * (level + 1) / 2 * 10;
                monsterXp[level] = level * (level + 1) / 2 * 100;
            }

            return new[]
            {
                new LevelTable(1, adventurerXp),
                new LevelTable(2, monsterXp)
            }.ToDictionary(x => x.Id);
        }

        static IReadOnlyDictionary<int, SpeciesMaster> CreateSpeciesMasters()
        {
            return new[]
            {
                new SpeciesMaster(
                    1,
                    "Goblin",
                    new[]
                    {
                        new ActorDropEntry(1002, 0.7f, 1, 1),
                        new ActorDropEntry(1, 0.5f, 1, 3),
                        new ActorDropEntry(3004, 1.0f, 1, 1)
                    }),
                new SpeciesMaster(
                    2,
                    "Orc",
                    new[]
                    {
                        new ActorDropEntry(1, 0.7f, 3, 8),
                        new ActorDropEntry(3004, 0.25f, 1, 1)
                    }),
                new SpeciesMaster(
                    3,
                    "Ogre",
                    new[]
                    {
                        new ActorDropEntry(1, 0.9f, 8, 15),
                        new ActorDropEntry(3004, 0.4f, 1, 1)
                    }),
                new SpeciesMaster(
                    10,
                    "Human",
                    Array.Empty<ActorDropEntry>())
            }.ToDictionary(x => x.Id);
        }

        static IReadOnlyDictionary<int, ActorArchetypeMaster> CreateActorArchetypeMasters()
        {
            return new[]
            {
                new ActorArchetypeMaster(
                    1,
                    "Novice Adventurer",
                    ActorBehaviorType.Adventurer,
                    10,
                    WeaponType.Fist,
                    new ActorStats(10, 8, 8, 8, 8, 8),
                    1,
                    1,
                    new[] { 3001, 3003 },
                    new[] { new ItemStack(SpecialItemIds.Money, 100), new ItemStack(2001, 1) }),
                new ActorArchetypeMaster(
                    2,
                    "Goblin",
                    ActorBehaviorType.Monster,
                    1,
                    WeaponType.Claws,
                    new ActorStats(3, 6, 3, 2, 2, 1),
                    1,
                    2,
                    Array.Empty<int>(),
                    Array.Empty<ItemStack>()),
                new ActorArchetypeMaster(
                    3,
                    "Orc",
                    ActorBehaviorType.Monster,
                    2,
                    WeaponType.Axe,
                    new ActorStats(8, 7, 8, 4, 4, 3),
                    1,
                    2,
                    Array.Empty<int>(),
                    Array.Empty<ItemStack>()),
                new ActorArchetypeMaster(
                    4,
                    "Ogre",
                    ActorBehaviorType.Monster,
                    3,
                    WeaponType.Fist,
                    new ActorStats(14, 8, 14, 6, 6, 8),
                    1,
                    2,
                    Array.Empty<int>(),
                    Array.Empty<ItemStack>())
            }.ToDictionary(x => x.Id);
        }

        static IReadOnlyDictionary<int, AdventurerSpawnMaster> CreateAdventurerSpawnMasters()
        {
            return new[]
            {
                new AdventurerSpawnMaster(1, "Alice", 1, true),
                new AdventurerSpawnMaster(2, "Bob", 1, true),
                new AdventurerSpawnMaster(3, "Cecilia", 1, true)
            }.ToDictionary(x => x.Id);
        }

        static IReadOnlyDictionary<int, SpawnTableMaster> CreateSpawnTableMasters()
        {
            var adventurerEntries = new[]
            {
                new SpawnTableEntryMaster(1, 1, 1, 100, 1, 1),
                new SpawnTableEntryMaster(2, 1, 2, 100, 1, 1),
                new SpawnTableEntryMaster(3, 1, 3, 100, 1, 1)
            };
            var monsterEntries = new[]
            {
                new SpawnTableEntryMaster(4, 2, 2, 100, 1, 5)
            };
            var floorTwoMonsterEntries = new[]
            {
                new SpawnTableEntryMaster(5, 3, 3, 100, 1, 5)
            };
            var floorThreeMonsterEntries = new[]
            {
                new SpawnTableEntryMaster(6, 4, 4, 100, 1, 5)
            };

            return new[]
            {
                new SpawnTableMaster(1, "Default Adventurer Spawn", SpawnTableTargetType.AdventurerSpawn, adventurerEntries),
                new SpawnTableMaster(2, "Dungeon Floor 1 Monster Spawn", SpawnTableTargetType.ActorArchetype, monsterEntries),
                new SpawnTableMaster(3, "Dungeon Floor 2 Monster Spawn", SpawnTableTargetType.ActorArchetype, floorTwoMonsterEntries),
                new SpawnTableMaster(4, "Dungeon Floor 3 Monster Spawn", SpawnTableTargetType.ActorArchetype, floorThreeMonsterEntries)
            }.ToDictionary(x => x.Id);
        }

        void ValidateReferences()
        {
            foreach (var equipmentMaster in equipmentMasters.Values)
            {
                RequireItem(equipmentMaster.ItemId);
            }

            foreach (var weaponMaster in weaponMasters.Values)
            {
                RequireItem(weaponMaster.ItemId);
                GetWeaponTypeCombatMaster(weaponMaster.WeaponType);
                if (!equipmentMasters.TryGetValue(weaponMaster.ItemId, out var equipmentMaster) ||
                    equipmentMaster.Slot != EquipmentSlot.Weapon)
                {
                    throw new InvalidOperationException("Weapon master requires weapon equipment master with same item id.");
                }
            }

            foreach (var archetypeMaster in actorArchetypeMasters.Values)
            {
                GetSpeciesMaster(archetypeMaster.SpeciesId);
                GetWeaponTypeCombatMaster(archetypeMaster.DefaultWeaponType);
                GetLevelTable(archetypeMaster.LevelTableId);
                ValidateItemIds(archetypeMaster.InitialEquipmentItemIds);
                ValidateItemStacks(archetypeMaster.InitialInventoryItemIds);
            }

            foreach (var adventurerSpawnMaster in adventurerSpawnMasters.Values)
            {
                var archetypeMaster = GetActorArchetypeMaster(adventurerSpawnMaster.ActorArchetypeId);
                if (archetypeMaster.BehaviorType != ActorBehaviorType.Adventurer)
                {
                    throw new InvalidOperationException("Adventurer spawn master requires adventurer actor archetype.");
                }
            }

            foreach (var speciesMaster in speciesMasters.Values)
            {
                foreach (var drop in speciesMaster.SpeciesDrops)
                {
                    RequireItem(drop.ItemId);
                }
            }

            foreach (var spawnTableMaster in spawnTableMasters.Values)
            {
                foreach (var entry in spawnTableMaster.Entries)
                {
                    ValidateSpawnTableEntry(spawnTableMaster, entry);
                }
            }

            foreach (var floorExplorationMaster in dungeonFloorExplorationMasters.Values)
            {
                var spawnTableMaster = GetSpawnTableMaster(floorExplorationMaster.MonsterSpawnTableId);
                if (spawnTableMaster.TargetType != SpawnTableTargetType.ActorArchetype)
                {
                    throw new InvalidOperationException("Dungeon floor exploration requires actor archetype spawn table.");
                }
            }
        }

        void ValidateSpawnTableEntry(SpawnTableMaster spawnTableMaster, SpawnTableEntryMaster entry)
        {
            if (entry.SpawnTableId != spawnTableMaster.Id)
            {
                throw new InvalidOperationException("Spawn table entry references different spawn table.");
            }

            switch (spawnTableMaster.TargetType)
            {
                case SpawnTableTargetType.ActorArchetype:
                    GetActorArchetypeMaster(entry.TargetId);
                    return;
                case SpawnTableTargetType.AdventurerSpawn:
                    GetAdventurerSpawnMaster(entry.TargetId);
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(spawnTableMaster));
            }
        }

        void ValidateItemIds(IEnumerable<int> itemIds)
        {
            foreach (var itemId in itemIds)
            {
                RequireItem(itemId);
            }
        }

        void ValidateItemStacks(IEnumerable<ItemStack> itemStacks)
        {
            foreach (var stack in itemStacks)
            {
                RequireItem(stack.ItemId);
            }
        }

        void RequireItem(int itemId)
        {
            GetItemMaster(itemId);
        }

        static TMaster GetRequired<TMaster>(
            IReadOnlyDictionary<int, TMaster> masters,
            int id,
            string masterName)
        {
            return GetRequired<int, TMaster>(masters, id, masterName);
        }

        static TMaster GetRequired<TKey, TMaster>(
            IReadOnlyDictionary<TKey, TMaster> masters,
            TKey id,
            string masterName)
        {
            if (masters.TryGetValue(id, out var master))
            {
                return master;
            }

            throw new KeyNotFoundException($"{masterName} does not exist. Id: {id}");
        }
    }
}
