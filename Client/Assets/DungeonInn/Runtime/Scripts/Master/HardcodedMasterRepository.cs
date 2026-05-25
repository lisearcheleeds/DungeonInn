using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
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
        readonly IReadOnlyDictionary<int, ActorEffectMaster> actorEffectMasters;
        readonly IReadOnlyDictionary<int, SpeciesMaster> speciesMasters;
        readonly IReadOnlyDictionary<int, SpawnTableMaster> spawnTableMasters;
        readonly IReadOnlyDictionary<int, LevelTable> levelTables;
        readonly IReadOnlyDictionary<int, DungeonFloorExplorationMaster> dungeonFloorExplorationMasters;
        readonly IReadOnlyDictionary<string, EnvironmentPropVisualMaster> environmentPropVisualMasters;
        readonly IReadOnlyDictionary<string, ActorVisualMaster> actorVisualMasters;

        public IReadOnlyDictionary<int, ItemMaster> ItemMasters => itemMasters;
        public IReadOnlyDictionary<int, EquipmentMaster> EquipmentMasters => equipmentMasters;
        public IReadOnlyDictionary<int, WeaponMaster> WeaponMasters => weaponMasters;
        public IReadOnlyDictionary<WeaponType, WeaponTypeCombatMaster> WeaponTypeCombatMasters => weaponTypeCombatMasters;
        public IReadOnlyDictionary<int, ActorArchetypeMaster> ActorArchetypeMasters => actorArchetypeMasters;
        public IReadOnlyDictionary<int, AdventurerSpawnMaster> AdventurerSpawnMasters => adventurerSpawnMasters;
        public IReadOnlyDictionary<int, ActorEffectMaster> ActorEffectMasters => actorEffectMasters;
        public IReadOnlyDictionary<int, SpeciesMaster> SpeciesMasters => speciesMasters;
        public IReadOnlyDictionary<int, SpawnTableMaster> SpawnTableMasters => spawnTableMasters;
        public IReadOnlyDictionary<int, LevelTable> LevelTables => levelTables;
        public IReadOnlyDictionary<int, DungeonFloorExplorationMaster> DungeonFloorExplorationMasters => dungeonFloorExplorationMasters;
        public IReadOnlyDictionary<string, EnvironmentPropVisualMaster> EnvironmentPropVisualMasters => environmentPropVisualMasters;
        public IReadOnlyDictionary<string, ActorVisualMaster> ActorVisualMasters => actorVisualMasters;

        public HardcodedMasterRepository()
        {
            actorEffectMasters = CreateActorEffectMasters();
            itemMasters = CreateItemMasters();
            equipmentMasters = CreateEquipmentMasters();
            weaponTypeCombatMasters = WeaponTypeCombatMasterCatalog.CreateAll();
            weaponMasters = CreateWeaponMasters();
            levelTables = CreateLevelTables();
            dungeonFloorExplorationMasters = CreateDungeonFloorExplorationMasters();
            environmentPropVisualMasters = CreateEnvironmentPropVisualMasters();
            actorVisualMasters = CreateActorVisualMasters();
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

        public ActorEffectMaster GetActorEffectMaster(int actorEffectId)
        {
            return GetRequired(actorEffectMasters, actorEffectId, nameof(ActorEffectMaster));
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

        public EnvironmentPropVisualMaster GetEnvironmentPropVisualMaster(string key)
        {
            return GetRequired(environmentPropVisualMasters, key, nameof(EnvironmentPropVisualMaster));
        }

        public ActorVisualMaster GetActorVisualMaster(string visualId, int skinId)
        {
            return GetRequired(actorVisualMasters, CreateActorVisualKey(visualId, skinId), nameof(ActorVisualMaster));
        }

        static IReadOnlyDictionary<string, EnvironmentPropVisualMaster> CreateEnvironmentPropVisualMasters()
        {
            return new[]
            {
                new EnvironmentPropVisualMaster("StairUp", "World/Prop/StairUp"),
                new EnvironmentPropVisualMaster("StairDown", "World/Prop/StairDown")
            }.ToDictionary(x => x.Key);
        }

        static IReadOnlyDictionary<string, ActorVisualMaster> CreateActorVisualMasters()
        {
            return new[]
            {
                new ActorVisualMaster("adventurer_novice", GameConstants.DefaultActorSkinId, "World/ActorVisual/AdventurerNovice"),
                new ActorVisualMaster("monster_goblin", GameConstants.DefaultActorSkinId, "World/ActorVisual/MonsterGoblin"),
                new ActorVisualMaster("monster_orc", GameConstants.DefaultActorSkinId, "World/ActorVisual/MonsterOrc"),
                new ActorVisualMaster("monster_ogre", GameConstants.DefaultActorSkinId, "World/ActorVisual/MonsterOgre"),
                new ActorVisualMaster(
                    "monster_goblin_archer",
                    GameConstants.DefaultActorSkinId,
                    "World/ActorVisual/MonsterGoblinArcher")
            }.ToDictionary(x => CreateActorVisualKey(x.VisualId, x.SkinId));
        }

        static IReadOnlyDictionary<int, ItemMaster> CreateItemMasters()
        {
            return new[]
            {
                new ItemMaster(1, "Gold", ItemTag.Currency, 1, 1, false, 100000),
                new ItemMaster(1001, "薬草", ItemTag.Material | ItemTag.Recovery, 10, 1, true, 10),
                new ItemMaster(1002, "Goblin Ear", ItemTag.Material | ItemTag.SellOnly, 25, 1, true, 10),
                new ItemMaster(1101, "鉄鉱石", ItemTag.Material | ItemTag.Ore, 35, 1, true, 20),
                new ItemMaster(1102, "銅鉱石", ItemTag.Material | ItemTag.Ore, 25, 1, true, 20),
                new ItemMaster(1103, "鉄", ItemTag.Material | ItemTag.Metal, 45, 2, true, 20),
                new ItemMaster(1104, "鋼", ItemTag.Material | ItemTag.Metal, 70, 3, true, 20),
                new ItemMaster(1105, "銅", ItemTag.Material | ItemTag.Metal, 35, 1, true, 20),
                new ItemMaster(1106, "ダイヤ", ItemTag.Material | ItemTag.Gem | ItemTag.Valuable, 300, 5, true, 10),
                new ItemMaster(1107, "ルビー", ItemTag.Material | ItemTag.Gem | ItemTag.Valuable, 220, 4, true, 10),
                new ItemMaster(1108, "サファイア", ItemTag.Material | ItemTag.Gem | ItemTag.Valuable, 220, 4, true, 10),
                new ItemMaster(1109, "パール", ItemTag.Material | ItemTag.Gem | ItemTag.Valuable, 160, 3, true, 10),
                new ItemMaster(1110, "汚い布", ItemTag.Material, 8, 1, true, 20),
                new ItemMaster(1111, "布", ItemTag.Material, 18, 1, true, 20),
                new ItemMaster(1112, "綺麗な布", ItemTag.Material | ItemTag.Valuable, 40, 2, true, 20),
                new ItemMaster(1113, "機械部品", ItemTag.Material | ItemTag.CraftingComponent, 90, 3, true, 20),
                new ItemMaster(1114, "電子部品", ItemTag.Material | ItemTag.CraftingComponent, 110, 3, true, 20),
                new ItemMaster(1115, "ねじ", ItemTag.Material | ItemTag.CraftingComponent, 12, 1, true, 50),
                new ItemMaster(1116, "歯車", ItemTag.Material | ItemTag.CraftingComponent, 30, 1, true, 30),
                new ItemMaster(1117, "レンズ", ItemTag.Material | ItemTag.CraftingComponent, 45, 2, true, 20),
                new ItemMaster(1118, "センサー", ItemTag.Material | ItemTag.CraftingComponent, 85, 3, true, 20),
                new ItemMaster(1119, "ポンプ", ItemTag.Material | ItemTag.CraftingComponent, 65, 2, true, 20),
                new ItemMaster(1120, "ライト", ItemTag.Material | ItemTag.CraftingComponent, 35, 1, true, 20),
                new ItemMaster(1121, "ボタン", ItemTag.Material | ItemTag.CraftingComponent, 15, 1, true, 50),
                new ItemMaster(1122, "プラスチック", ItemTag.Material | ItemTag.CraftingComponent, 20, 1, true, 30),
                new ItemMaster(1201, "ピッケル", ItemTag.Tool, 80, 2, true, 1),
                new ItemMaster(1202, "スコップ", ItemTag.Tool, 60, 1, true, 1),
                new ItemMaster(1203, "ドライバー", ItemTag.Tool | ItemTag.CraftingComponent, 45, 1, true, 1),
                new ItemMaster(2001, "ポーション", ItemTag.Recovery, 30, 1, true, 10, 1),
                new ItemMaster(2002, "ハイポーション", ItemTag.Recovery, 90, 2, true, 10, 1),
                new ItemMaster(2003, "エリクサー", ItemTag.Recovery | ItemTag.ManaRecovery | ItemTag.Valuable, 300, 5, true, 5, 1),
                new ItemMaster(2004, "マナポーション", ItemTag.ManaRecovery, 50, 1, true, 10),
                new ItemMaster(2101, "ゆでたまご", ItemTag.Food, 8, 1, true, 20),
                new ItemMaster(2102, "おにぎり", ItemTag.Food, 12, 1, true, 20),
                new ItemMaster(2103, "ビール", ItemTag.Drink, 20, 1, true, 20),
                new ItemMaster(2104, "肉", ItemTag.Food | ItemTag.Material, 25, 1, true, 20),
                new ItemMaster(2105, "謎の肉", ItemTag.Food | ItemTag.Material, 18, 1, true, 20),
                new ItemMaster(2106, "魚", ItemTag.Food | ItemTag.Material, 20, 1, true, 20),
                new ItemMaster(2107, "魚の串焼き", ItemTag.Food, 35, 1, true, 20),
                new ItemMaster(2108, "肉の串焼き", ItemTag.Food, 40, 1, true, 20),
                new ItemMaster(2109, "ラーメン", ItemTag.Food, 50, 2, true, 20),
                new ItemMaster(2110, "コロッケ", ItemTag.Food, 18, 1, true, 20),
                new ItemMaster(2111, "ポテト", ItemTag.Food, 14, 1, true, 20),
                new ItemMaster(2112, "からあげ", ItemTag.Food, 28, 1, true, 20),
                new ItemMaster(3001, "木の剣", ItemTag.Weapon, 80, 1, true, 1),
                new ItemMaster(3002, "木の弓", ItemTag.Weapon, 80, 1, true, 1),
                new ItemMaster(3003, "布", ItemTag.Armor | ItemTag.Material, 60, 1, true, 1),
                new ItemMaster(3004, "鉄の剣", ItemTag.Weapon, 120, 5, true, 1),
                new ItemMaster(3005, "鉄の斧", ItemTag.Weapon, 130, 5, true, 1),
                new ItemMaster(3006, "鉄の大鎌", ItemTag.Weapon, 150, 5, true, 1),
                new ItemMaster(3007, "鉄の杖", ItemTag.Weapon, 120, 5, true, 1),
                new ItemMaster(3008, "木の斧", ItemTag.Weapon, 75, 1, true, 1),
                new ItemMaster(3009, "木の大鎌", ItemTag.Weapon, 90, 1, true, 1),
                new ItemMaster(3010, "木の杖", ItemTag.Weapon, 70, 1, true, 1),
                new ItemMaster(3011, "木の棒", ItemTag.Weapon, 20, 1, true, 1),
                new ItemMaster(3012, "鉄の弓", ItemTag.Weapon, 130, 5, true, 1),
                new ItemMaster(4001, "ダイヤのアクセサリー", ItemTag.Accessory | ItemTag.Gem | ItemTag.Valuable, 420, 5, true, 1),
                new ItemMaster(4002, "ルビーのアクセサリー", ItemTag.Accessory | ItemTag.Gem | ItemTag.Valuable, 340, 4, true, 1),
                new ItemMaster(4003, "サファイアのアクセサリー", ItemTag.Accessory | ItemTag.Gem | ItemTag.Valuable, 340, 4, true, 1),
                new ItemMaster(4004, "パールのアクセサリー", ItemTag.Accessory | ItemTag.Gem | ItemTag.Valuable, 260, 3, true, 1)
            }.ToDictionary(x => x.Id);
        }

        static IReadOnlyDictionary<int, ActorEffectMaster> CreateActorEffectMasters()
        {
            return new[]
            {
                new ActorEffectMaster(
                    1,
                    "体力回復ポーション",
                    10f,
                    ActorEffectReapplyPolicy.AppendDuration,
                    new[]
                    {
                        new StatusEffectSpec(
                            StatusEffectType.HealHpOverTime,
                            30,
                            10f,
                            1f,
                            StatusEffectAggregationPolicy.Sum)
                    })
            }.ToDictionary(x => x.Id);
        }

        static IReadOnlyDictionary<int, DungeonFloorExplorationMaster> CreateDungeonFloorExplorationMasters()
        {
            return new[]
            {
                new DungeonFloorExplorationMaster(1, 2, 3.0f),
                new DungeonFloorExplorationMaster(2, 3, 3.0f),
                new DungeonFloorExplorationMaster(3, 4, 3.0f)
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
                }),
                new EquipmentMaster(3005, EquipmentSlot.Weapon, 0, new[]
                {
                    new StatBonus(StatType.Strength, 6)
                }),
                new EquipmentMaster(3006, EquipmentSlot.Weapon, 0, new[]
                {
                    new StatBonus(StatType.Strength, 4),
                    new StatBonus(StatType.Dexterity, 3)
                }),
                new EquipmentMaster(3007, EquipmentSlot.Weapon, 0, new[]
                {
                    new StatBonus(StatType.Intelligence, 5),
                    new StatBonus(StatType.Wisdom, 2)
                }),
                new EquipmentMaster(3008, EquipmentSlot.Weapon, 0, new[]
                {
                    new StatBonus(StatType.Strength, 3)
                }),
                new EquipmentMaster(3009, EquipmentSlot.Weapon, 0, new[]
                {
                    new StatBonus(StatType.Strength, 2),
                    new StatBonus(StatType.Dexterity, 2)
                }),
                new EquipmentMaster(3010, EquipmentSlot.Weapon, 0, new[]
                {
                    new StatBonus(StatType.Intelligence, 3),
                    new StatBonus(StatType.Wisdom, 1)
                }),
                new EquipmentMaster(3011, EquipmentSlot.Weapon, 0, new[]
                {
                    new StatBonus(StatType.Intelligence, 1)
                }),
                new EquipmentMaster(3012, EquipmentSlot.Weapon, 0, new[]
                {
                    new StatBonus(StatType.Dexterity, 5),
                    new StatBonus(StatType.Strength, 2)
                }),
                new EquipmentMaster(4001, EquipmentSlot.Accessory, 0, new[]
                {
                    new StatBonus(StatType.Charisma, 4),
                    new StatBonus(StatType.Wisdom, 2)
                }),
                new EquipmentMaster(4002, EquipmentSlot.Accessory, 0, new[]
                {
                    new StatBonus(StatType.Strength, 3),
                    new StatBonus(StatType.Charisma, 1)
                }),
                new EquipmentMaster(4003, EquipmentSlot.Accessory, 0, new[]
                {
                    new StatBonus(StatType.Intelligence, 3),
                    new StatBonus(StatType.Wisdom, 1)
                }),
                new EquipmentMaster(4004, EquipmentSlot.Accessory, 0, new[]
                {
                    new StatBonus(StatType.Constitution, 2),
                    new StatBonus(StatType.Charisma, 1)
                })
            }.ToDictionary(x => x.ItemId);
        }

        IReadOnlyDictionary<int, WeaponMaster> CreateWeaponMasters()
        {
            return new[]
            {
                new WeaponMaster(3001, WeaponType.Sword, GetWeaponTypeCombatMaster(WeaponType.Sword), 8, 0, 0),
                new WeaponMaster(3002, WeaponType.Bow, GetWeaponTypeCombatMaster(WeaponType.Bow), 7, 0, 0),
                new WeaponMaster(3004, WeaponType.Sword, GetWeaponTypeCombatMaster(WeaponType.Sword), 10, 0, 0),
                new WeaponMaster(3005, WeaponType.Axe, GetWeaponTypeCombatMaster(WeaponType.Axe), 12, 0, 0),
                new WeaponMaster(3006, WeaponType.Scythe, GetWeaponTypeCombatMaster(WeaponType.Scythe), 11, 0, 0),
                new WeaponMaster(3007, WeaponType.Staff, GetWeaponTypeCombatMaster(WeaponType.Staff), 9, 0, 0),
                new WeaponMaster(3008, WeaponType.Axe, GetWeaponTypeCombatMaster(WeaponType.Axe), 7, 0, 0),
                new WeaponMaster(3009, WeaponType.Scythe, GetWeaponTypeCombatMaster(WeaponType.Scythe), 8, 0, 0),
                new WeaponMaster(3010, WeaponType.Staff, GetWeaponTypeCombatMaster(WeaponType.Staff), 6, 0, 0),
                new WeaponMaster(3011, WeaponType.Staff, GetWeaponTypeCombatMaster(WeaponType.Staff), 2, 0, 0),
                new WeaponMaster(3012, WeaponType.Bow, GetWeaponTypeCombatMaster(WeaponType.Bow), 10, 0, 0)
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
                    "adventurer_novice",
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
                    "monster_goblin",
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
                    "monster_orc",
                    ActorBehaviorType.Monster,
                    2,
                    WeaponType.Scythe,
                    new ActorStats(8, 7, 8, 4, 4, 3),
                    1,
                    2,
                    Array.Empty<int>(),
                    Array.Empty<ItemStack>()),
                new ActorArchetypeMaster(
                    4,
                    "Ogre",
                    "monster_ogre",
                    ActorBehaviorType.Monster,
                    3,
                    WeaponType.Fist,
                    new ActorStats(14, 8, 14, 6, 6, 8),
                    1,
                    2,
                    Array.Empty<int>(),
                    Array.Empty<ItemStack>()),
                new ActorArchetypeMaster(
                    5,
                    "Goblin Archer",
                    "monster_goblin_archer",
                    ActorBehaviorType.Monster,
                    1,
                    WeaponType.Bow,
                    new ActorStats(3, 9, 3, 2, 2, 1),
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
                new AdventurerSpawnMaster(1, "アリス", 1, true),
                new AdventurerSpawnMaster(2, "ヤスオ", 1, true),
                new AdventurerSpawnMaster(3, "アカリ", 1, true),
                new AdventurerSpawnMaster(4, "ゼド", 1, true),
                new AdventurerSpawnMaster(5, "ラックス", 1, true),
                new AdventurerSpawnMaster(6, "ガレン", 1, true),
                new AdventurerSpawnMaster(7, "セト", 1, true),
                new AdventurerSpawnMaster(8, "ユーミ", 1, true),
            }.ToDictionary(x => x.Id);
        }

        static IReadOnlyDictionary<int, SpawnTableMaster> CreateSpawnTableMasters()
        {
            var adventurerEntries = new[]
            {
                new SpawnTableEntryMaster(1, 1, 1, 100, 1, 1),
                new SpawnTableEntryMaster(2, 1, 2, 100, 1, 1),
                new SpawnTableEntryMaster(3, 1, 3, 100, 1, 1),
                new SpawnTableEntryMaster(4, 1, 4, 100, 1, 1),
                new SpawnTableEntryMaster(5, 1, 5, 100, 1, 1),
                new SpawnTableEntryMaster(6, 1, 6, 100, 1, 1),
                new SpawnTableEntryMaster(7, 1, 7, 100, 1, 1),
                new SpawnTableEntryMaster(8, 1, 8, 100, 1, 1)
            };
            var monsterEntries = new[]
            {
                new SpawnTableEntryMaster(4, 2, 2, 70, 1, 5),
                new SpawnTableEntryMaster(7, 2, 5, 30, 1, 5)
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
            foreach (var itemMaster in itemMasters.Values)
            {
                if (0 < itemMaster.ActorEffectMasterId)
                {
                    GetActorEffectMaster(itemMaster.ActorEffectMasterId);
                }
            }

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
                GetActorVisualMaster(archetypeMaster.VisualId, GameConstants.DefaultActorSkinId);
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
                    GetActorArchetypeMaster(entry.TargetMasterId);
                    return;
                case SpawnTableTargetType.AdventurerSpawn:
                    GetAdventurerSpawnMaster(entry.TargetMasterId);
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

        static string CreateActorVisualKey(string visualId, int skinId)
        {
            return $"{visualId}:{skinId}";
        }
    }
}
