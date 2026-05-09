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
        readonly IReadOnlyDictionary<int, MonsterSpeciesMaster> monsterSpeciesMasters;
        readonly IReadOnlyDictionary<int, SpawnTableMaster> spawnTableMasters;
        readonly IReadOnlyDictionary<int, LevelTable> levelTables;

        public IReadOnlyDictionary<int, ItemMaster> ItemMasters => itemMasters;
        public IReadOnlyDictionary<int, EquipmentMaster> EquipmentMasters => equipmentMasters;
        public IReadOnlyDictionary<int, WeaponMaster> WeaponMasters => weaponMasters;
        public IReadOnlyDictionary<WeaponType, WeaponTypeCombatMaster> WeaponTypeCombatMasters => weaponTypeCombatMasters;
        public IReadOnlyDictionary<int, ActorArchetypeMaster> ActorArchetypeMasters => actorArchetypeMasters;
        public IReadOnlyDictionary<int, MonsterSpeciesMaster> MonsterSpeciesMasters => monsterSpeciesMasters;
        public IReadOnlyDictionary<int, SpawnTableMaster> SpawnTableMasters => spawnTableMasters;
        public IReadOnlyDictionary<int, LevelTable> LevelTables => levelTables;

        public HardcodedMasterRepository()
        {
            itemMasters = CreateItemMasters();
            equipmentMasters = CreateEquipmentMasters();
            weaponTypeCombatMasters = WeaponTypeCombatMasterCatalog.CreateAll();
            weaponMasters = CreateWeaponMasters();
            levelTables = CreateLevelTables();
            actorArchetypeMasters = CreateActorArchetypeMasters();
            monsterSpeciesMasters = CreateMonsterSpeciesMasters();
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

        public MonsterSpeciesMaster GetMonsterSpeciesMaster(int speciesId)
        {
            return GetRequired(monsterSpeciesMasters, speciesId, nameof(MonsterSpeciesMaster));
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
                new ItemMaster(3003, "Cloth Armor", ItemCategory.Equipment, 60, 1, true, 1)
            }.ToDictionary(x => x.Id);
        }

        static IReadOnlyDictionary<int, EquipmentMaster> CreateEquipmentMasters()
        {
            return new[]
            {
                new EquipmentMaster(3001, EquipmentSlot.Weapon, 0),
                new EquipmentMaster(3002, EquipmentSlot.Weapon, 0),
                new EquipmentMaster(3003, EquipmentSlot.Armor, 4)
            }.ToDictionary(x => x.ItemId);
        }

        static IReadOnlyDictionary<int, WeaponMaster> CreateWeaponMasters()
        {
            return new[]
            {
                new WeaponMaster(3001, WeaponType.Sword, 8, 0, 0),
                new WeaponMaster(3002, WeaponType.Bow, 7, 0, 0)
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

        static IReadOnlyDictionary<int, ActorArchetypeMaster> CreateActorArchetypeMasters()
        {
            return new[]
            {
                new ActorArchetypeMaster(
                    1,
                    "Novice Adventurer",
                    ActorBehaviorType.Adventurer,
                    new ActorStats(5, 5, 5, 5, 5, 5),
                    1,
                    1,
                    new[] { 3001, 3003 },
                    new[] { new ItemStack(SpecialItemIds.Money, 100), new ItemStack(2001, 1) }),
                new ActorArchetypeMaster(
                    2,
                    "Goblin",
                    ActorBehaviorType.Monster,
                    new ActorStats(4, 6, 4, 2, 2, 1),
                    1,
                    2,
                    Array.Empty<int>(),
                    Array.Empty<ItemStack>())
            }.ToDictionary(x => x.Id);
        }

        static IReadOnlyDictionary<int, MonsterSpeciesMaster> CreateMonsterSpeciesMasters()
        {
            return new[]
            {
                new MonsterSpeciesMaster(
                    1,
                    "Goblin",
                    2,
                    WeaponType.Claws,
                    true,
                    new[]
                    {
                        new ActorDropEntry(1002, 0.7f, 1, 1),
                        new ActorDropEntry(1, 0.5f, 1, 3)
                    })
            }.ToDictionary(x => x.Id);
        }

        static IReadOnlyDictionary<int, SpawnTableMaster> CreateSpawnTableMasters()
        {
            var adventurerEntries = new[]
            {
                new SpawnTableEntryMaster(1, 1, 1, 100, 1, 1)
            };
            var monsterEntries = new[]
            {
                new SpawnTableEntryMaster(2, 2, 1, 100, 1, 5)
            };

            return new[]
            {
                new SpawnTableMaster(1, "Default Adventurer Spawn", SpawnTableTargetType.ActorArchetype, adventurerEntries),
                new SpawnTableMaster(2, "Dungeon Floor 1 Monster Spawn", SpawnTableTargetType.MonsterSpecies, monsterEntries)
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
                GetLevelTable(archetypeMaster.LevelTableId);
                ValidateItemIds(archetypeMaster.InitialEquipmentItemIds);
                ValidateItemStacks(archetypeMaster.InitialInventoryItemIds);
            }

            foreach (var speciesMaster in monsterSpeciesMasters.Values)
            {
                GetActorArchetypeMaster(speciesMaster.ActorArchetypeId);
                GetWeaponTypeCombatMaster(speciesMaster.DefaultWeaponType);
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
                case SpawnTableTargetType.MonsterSpecies:
                    GetMonsterSpeciesMaster(entry.TargetId);
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
