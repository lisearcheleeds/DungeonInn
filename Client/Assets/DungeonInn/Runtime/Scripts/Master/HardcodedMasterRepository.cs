using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Item;

namespace DungeonInn.Master
{
    public sealed class HardcodedMasterRepository :
        IMasterRepository,
        IFacilityUpgradeMasterRepository,
        IFacilityLineupMasterRepository,
        IMarketOfferMasterRepository,
        IItemStackLimitResolver
    {
        readonly IReadOnlyDictionary<int, ItemMaster> itemMasters;
        readonly IReadOnlyDictionary<int, EquipmentMaster> equipmentMasters;
        readonly IReadOnlyDictionary<int, WeaponMaster> weaponMasters;
        readonly IReadOnlyDictionary<WeaponType, WeaponTypeCombatMaster> weaponTypeCombatMasters;
        readonly IReadOnlyDictionary<int, ActorArchetypeMaster> actorArchetypeMasters;
        readonly IReadOnlyDictionary<int, ActorLoadoutMaster> actorLoadoutMasters;
        readonly IReadOnlyDictionary<int, AdventurerSpawnMaster> adventurerSpawnMasters;
        readonly IReadOnlyDictionary<int, AdventurerSpawnBandMaster> adventurerSpawnBandMasters;
        readonly IReadOnlyDictionary<int, ActorEffectMaster> actorEffectMasters;
        readonly IReadOnlyDictionary<int, SpeciesMaster> speciesMasters;
        readonly IReadOnlyDictionary<int, SpawnTableMaster> spawnTableMasters;
        readonly IReadOnlyDictionary<int, LevelTable> levelTables;
        readonly IReadOnlyDictionary<int, DungeonDepthBandMaster> dungeonDepthBandMasters;
        readonly IReadOnlyDictionary<string, EnvironmentPropVisualMaster> environmentPropVisualMasters;
        readonly IReadOnlyDictionary<string, ActorVisualMaster> actorVisualMasters;
        readonly IReadOnlyDictionary<int, FacilityUpgradeMaster> facilityUpgradeMasters;
        readonly IReadOnlyDictionary<(FacilityType FacilityType, int FromLevel), FacilityUpgradeMaster>
            facilityUpgradeMasterByFacilityAndLevel;
        readonly IReadOnlyDictionary<int, FacilityLineupMaster> facilityLineupMasters;
        readonly IReadOnlyDictionary<int, FacilityLineupItemMaster> facilityLineupItemMasters;
        readonly IReadOnlyDictionary<int, MarketOfferMaster> marketOfferMasters;

        public IReadOnlyDictionary<int, ItemMaster> ItemMasters => itemMasters;
        public IReadOnlyDictionary<int, EquipmentMaster> EquipmentMasters => equipmentMasters;
        public IReadOnlyDictionary<int, WeaponMaster> WeaponMasters => weaponMasters;
        public IReadOnlyDictionary<WeaponType, WeaponTypeCombatMaster> WeaponTypeCombatMasters => weaponTypeCombatMasters;
        public IReadOnlyDictionary<int, ActorArchetypeMaster> ActorArchetypeMasters => actorArchetypeMasters;
        public IReadOnlyDictionary<int, ActorLoadoutMaster> ActorLoadoutMasters => actorLoadoutMasters;
        public IReadOnlyDictionary<int, AdventurerSpawnMaster> AdventurerSpawnMasters => adventurerSpawnMasters;
        public IReadOnlyDictionary<int, AdventurerSpawnBandMaster> AdventurerSpawnBandMasters => adventurerSpawnBandMasters;
        public IReadOnlyDictionary<int, ActorEffectMaster> ActorEffectMasters => actorEffectMasters;
        public IReadOnlyDictionary<int, SpeciesMaster> SpeciesMasters => speciesMasters;
        public IReadOnlyDictionary<int, SpawnTableMaster> SpawnTableMasters => spawnTableMasters;
        public IReadOnlyDictionary<int, LevelTable> LevelTables => levelTables;
        public IReadOnlyDictionary<int, DungeonDepthBandMaster> DungeonDepthBandMasters => dungeonDepthBandMasters;
        public IReadOnlyDictionary<string, EnvironmentPropVisualMaster> EnvironmentPropVisualMasters => environmentPropVisualMasters;
        public IReadOnlyDictionary<string, ActorVisualMaster> ActorVisualMasters => actorVisualMasters;
        public IReadOnlyDictionary<int, FacilityUpgradeMaster> FacilityUpgradeMasters => facilityUpgradeMasters;
        public IReadOnlyDictionary<int, FacilityLineupMaster> FacilityLineupMasters => facilityLineupMasters;
        public IReadOnlyDictionary<int, FacilityLineupItemMaster> FacilityLineupItemMasters => facilityLineupItemMasters;
        public IReadOnlyDictionary<int, MarketOfferMaster> MarketOfferMasters => marketOfferMasters;

        public HardcodedMasterRepository()
        {
            actorEffectMasters = CreateActorEffectMasters();
            itemMasters = CreateItemMasters();
            equipmentMasters = CreateEquipmentMasters();
            weaponTypeCombatMasters = WeaponTypeCombatMasterCatalog.CreateAll();
            weaponMasters = CreateWeaponMasters();
            actorLoadoutMasters = CreateActorLoadoutMasters();
            levelTables = CreateLevelTables();
            dungeonDepthBandMasters = CreateDungeonDepthBandMasters();
            environmentPropVisualMasters = CreateEnvironmentPropVisualMasters();
            actorVisualMasters = CreateActorVisualMasters();
            facilityUpgradeMasters = CreateFacilityUpgradeMasters();
            facilityUpgradeMasterByFacilityAndLevel = facilityUpgradeMasters.Values
                .ToDictionary(x => (x.FacilityType, x.FromLevel));
            facilityLineupMasters = CreateFacilityLineupMasters();
            facilityLineupItemMasters = CreateFacilityLineupItemMasters();
            marketOfferMasters = CreateMarketOfferMasters();
            speciesMasters = CreateSpeciesMasters();
            actorArchetypeMasters = CreateActorArchetypeMasters();
            adventurerSpawnMasters = CreateAdventurerSpawnMasters();
            spawnTableMasters = CreateSpawnTableMasters();
            adventurerSpawnBandMasters = CreateAdventurerSpawnBandMasters();
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

        public ActorLoadoutMaster GetActorLoadoutMaster(int loadoutId)
        {
            return GetRequired(actorLoadoutMasters, loadoutId, nameof(ActorLoadoutMaster));
        }

        public AdventurerSpawnMaster GetAdventurerSpawnMaster(int adventurerSpawnId)
        {
            return GetRequired(adventurerSpawnMasters, adventurerSpawnId, nameof(AdventurerSpawnMaster));
        }

        public AdventurerSpawnBandMaster GetAdventurerSpawnBandMaster(int currentDay)
        {
            AdventurerSpawnBandMaster selectedMaster = null;
            foreach (var master in adventurerSpawnBandMasters.Values)
            {
                if (currentDay < master.MinDay)
                {
                    continue;
                }

                if (selectedMaster == null || selectedMaster.Priority < master.Priority)
                {
                    selectedMaster = master;
                }
            }

            if (selectedMaster != null)
            {
                return selectedMaster;
            }

            throw new KeyNotFoundException($"{nameof(AdventurerSpawnBandMaster)} does not exist. Day: {currentDay}");
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

        public DungeonDepthBandMaster GetDungeonDepthBandMasterForFloor(int floorIndex)
        {
            DungeonDepthBandMaster selectedMaster = null;
            foreach (var master in dungeonDepthBandMasters.Values)
            {
                if (!master.Contains(floorIndex))
                {
                    continue;
                }

                if (selectedMaster == null ||
                    selectedMaster.SelectionPriority < master.SelectionPriority)
                {
                    selectedMaster = master;
                }
            }

            if (selectedMaster != null)
            {
                return selectedMaster;
            }

            throw new KeyNotFoundException($"{nameof(DungeonDepthBandMaster)} does not exist. Floor: {floorIndex}");
        }

        public EnvironmentPropVisualMaster GetEnvironmentPropVisualMaster(string key)
        {
            return GetRequired(environmentPropVisualMasters, key, nameof(EnvironmentPropVisualMaster));
        }

        public ActorVisualMaster GetActorVisualMaster(string visualId, int skinId)
        {
            return GetRequired(actorVisualMasters, CreateActorVisualKey(visualId, skinId), nameof(ActorVisualMaster));
        }

        public bool TryGetFacilityUpgradeMaster(
            FacilityType facilityType,
            int fromLevel,
            out FacilityUpgradeMaster master)
        {
            return facilityUpgradeMasterByFacilityAndLevel.TryGetValue((facilityType, fromLevel), out master);
        }

        public MarketOfferMaster GetMarketOfferMaster(int offerId)
        {
            return GetRequired(marketOfferMasters, offerId, nameof(MarketOfferMaster));
        }

        static IReadOnlyDictionary<int, FacilityUpgradeMaster> CreateFacilityUpgradeMasters()
        {
            return new[]
            {
                new FacilityUpgradeMaster(
                    1,
                    FacilityType.Inn,
                    1,
                    2,
                    2,
                    2,
                    new[] { new ItemStack(SpecialItemIds.Money, 1000) }),
                new FacilityUpgradeMaster(
                    2,
                    FacilityType.GeneralStore,
                    1,
                    2,
                    2,
                    1,
                    new[] { new ItemStack(SpecialItemIds.Money, 1000) }),
                new FacilityUpgradeMaster(
                    3,
                    FacilityType.EquipmentShop,
                    1,
                    2,
                    2,
                    1,
                    new[] { new ItemStack(SpecialItemIds.Money, 1000) })
            }.ToDictionary(x => x.Id);
        }

        static IReadOnlyDictionary<int, FacilityLineupMaster> CreateFacilityLineupMasters()
        {
            return new[]
            {
                new FacilityLineupMaster(1, FacilityType.GeneralStore, 1, 1),
                new FacilityLineupMaster(2, FacilityType.GeneralStore, 2, 2),
                new FacilityLineupMaster(3, FacilityType.EquipmentShop, 1, 1),
                new FacilityLineupMaster(4, FacilityType.EquipmentShop, 2, 2)
            }.ToDictionary(x => x.Id);
        }

        static IReadOnlyDictionary<int, FacilityLineupItemMaster> CreateFacilityLineupItemMasters()
        {
            return new[]
            {
                new FacilityLineupItemMaster(1, 1, 2001, 1),
                new FacilityLineupItemMaster(2, 1, 2101, 2),
                new FacilityLineupItemMaster(3, 2, 2002, 1),
                new FacilityLineupItemMaster(4, 2, 2108, 2),
                new FacilityLineupItemMaster(5, 3, 3001, 1),
                new FacilityLineupItemMaster(6, 3, 3002, 2),
                new FacilityLineupItemMaster(7, 4, 3004, 1),
                new FacilityLineupItemMaster(8, 4, 3012, 2)
            }.ToDictionary(x => x.Id);
        }

        static IReadOnlyDictionary<int, MarketOfferMaster> CreateMarketOfferMasters()
        {
            return new[]
            {
                new MarketOfferMaster(
                    1,
                    3,
                    1,
                    new[] { new ItemStack(1002, 10) },
                    new[] { new ItemStack(SpecialItemIds.Money, 1000) }),
                new MarketOfferMaster(
                    2,
                    4,
                    2,
                    new[] { new ItemStack(1001, 20) },
                    new[] { new ItemStack(SpecialItemIds.Money, 800) }),
                new MarketOfferMaster(
                    3,
                    5,
                    3,
                    new[] { new ItemStack(1101, 5) },
                    new[] { new ItemStack(SpecialItemIds.Money, 1200) })
            }.ToDictionary(x => x.Id);
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
                new ActorVisualMaster("monster_slime", GameConstants.DefaultActorSkinId, "World/ActorVisual/MonsterGoblin"),
                new ActorVisualMaster("monster_wolf", GameConstants.DefaultActorSkinId, "World/ActorVisual/MonsterGoblin"),
                new ActorVisualMaster("monster_skeleton", GameConstants.DefaultActorSkinId, "World/ActorVisual/MonsterGoblin"),
                new ActorVisualMaster("monster_bat", GameConstants.DefaultActorSkinId, "World/ActorVisual/MonsterGoblinArcher"),
                new ActorVisualMaster("monster_golem", GameConstants.DefaultActorSkinId, "World/ActorVisual/MonsterOgre"),
                new ActorVisualMaster("monster_dragonkin", GameConstants.DefaultActorSkinId, "World/ActorVisual/MonsterOgre"),
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
                new ItemMaster(1003, "Slime Gel", ItemTag.Material | ItemTag.CraftingComponent, 12, 1, true, 20),
                new ItemMaster(1004, "Wolf Fang", ItemTag.Material | ItemTag.CraftingComponent, 28, 1, true, 20),
                new ItemMaster(1005, "Bat Wing", ItemTag.Material | ItemTag.CraftingComponent, 24, 1, true, 20),
                new ItemMaster(1006, "Bone Shard", ItemTag.Material | ItemTag.CraftingComponent, 32, 2, true, 20),
                new ItemMaster(1007, "Golem Core", ItemTag.Material | ItemTag.Valuable | ItemTag.CraftingComponent, 180, 4, true, 10),
                new ItemMaster(1008, "Dragon Scale", ItemTag.Material | ItemTag.Valuable | ItemTag.CraftingComponent, 260, 5, true, 10),
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
                new ItemMaster(2002, "ハイポーション", ItemTag.Recovery, 90, 2, true, 10, 2),
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
                new ItemMaster(3013, "木の短剣", ItemTag.Weapon, 70, 1, true, 1),
                new ItemMaster(3014, "鉄の短剣", ItemTag.Weapon, 125, 5, true, 1),
                new ItemMaster(3101, "革鎧", ItemTag.Armor, 90, 1, true, 1),
                new ItemMaster(3102, "鉄鎧", ItemTag.Armor | ItemTag.Metal, 180, 4, true, 1),
                new ItemMaster(3103, "ローブ", ItemTag.Armor, 110, 2, true, 1),
                new ItemMaster(4001, "ダイヤのアクセサリー", ItemTag.Accessory | ItemTag.Gem | ItemTag.Valuable, 420, 5, true, 1),
                new ItemMaster(4002, "ルビーのアクセサリー", ItemTag.Accessory | ItemTag.Gem | ItemTag.Valuable, 340, 4, true, 1),
                new ItemMaster(4003, "サファイアのアクセサリー", ItemTag.Accessory | ItemTag.Gem | ItemTag.Valuable, 340, 4, true, 1),
                new ItemMaster(4004, "パールのアクセサリー", ItemTag.Accessory | ItemTag.Gem | ItemTag.Valuable, 260, 3, true, 1),
                new ItemMaster(4005, "守りの指輪", ItemTag.Accessory, 160, 3, true, 1),
                new ItemMaster(4006, "知恵の護符", ItemTag.Accessory, 160, 3, true, 1)
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
                    }),
                new ActorEffectMaster(
                    2,
                    "体力回復ハイポーション",
                    10f,
                    ActorEffectReapplyPolicy.AppendDuration,
                    new[]
                    {
                        new StatusEffectSpec(
                            StatusEffectType.HealHpOverTime,
                            60,
                            10f,
                            1f,
                            StatusEffectAggregationPolicy.Sum)
                    })
            }.ToDictionary(x => x.Id);
        }

        static IReadOnlyDictionary<int, DungeonDepthBandMaster> CreateDungeonDepthBandMasters()
        {
            return new[]
            {
                new DungeonDepthBandMaster(
                    1,
                    "Shallow",
                    1,
                    3,
                    0,
                    10,
                    2,
                    1,
                    1.0f,
                    DungeonSpecialRoomType.None,
                    new DungeonFloorGenerationSettings(0)),
                new DungeonDepthBandMaster(
                    2,
                    "Middle",
                    4,
                    7,
                    0,
                    10,
                    3,
                    3,
                    1.35f,
                    DungeonSpecialRoomType.TreasureRoom,
                    new DungeonFloorGenerationSettings(1)),
                new DungeonDepthBandMaster(
                    3,
                    "Deep",
                    8,
                    11,
                    0,
                    10,
                    4,
                    6,
                    1.75f,
                    DungeonSpecialRoomType.RestRoom,
                    new DungeonFloorGenerationSettings(2)),
                new DungeonDepthBandMaster(
                    4,
                    "Boss",
                    5,
                    0,
                    5,
                    100,
                    5,
                    8,
                    2.2f,
                    DungeonSpecialRoomType.BossRoom,
                    new DungeonFloorGenerationSettings(3)),
                new DungeonDepthBandMaster(
                    5,
                    "Endless",
                    12,
                    0,
                    0,
                    20,
                    6,
                    10,
                    2.0f,
                    DungeonSpecialRoomType.None,
                    new DungeonFloorGenerationSettings(4))
            }.ToDictionary(x => x.Id);
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
                new EquipmentMaster(3013, EquipmentSlot.Weapon, 0, new[]
                {
                    new StatBonus(StatType.Dexterity, 3),
                    new StatBonus(StatType.Strength, 1)
                }),
                new EquipmentMaster(3014, EquipmentSlot.Weapon, 0, new[]
                {
                    new StatBonus(StatType.Dexterity, 5),
                    new StatBonus(StatType.Strength, 2)
                }),
                new EquipmentMaster(3101, EquipmentSlot.Armor, 3, new[]
                {
                    new StatBonus(StatType.Dexterity, 1),
                    new StatBonus(StatType.Constitution, 1)
                }),
                new EquipmentMaster(3102, EquipmentSlot.Armor, 8, new[]
                {
                    new StatBonus(StatType.Constitution, 3),
                    new StatBonus(StatType.Strength, 1)
                }),
                new EquipmentMaster(3103, EquipmentSlot.Armor, 2, new[]
                {
                    new StatBonus(StatType.Intelligence, 2),
                    new StatBonus(StatType.Wisdom, 2)
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
                }),
                new EquipmentMaster(4005, EquipmentSlot.Accessory, 0, new[]
                {
                    new StatBonus(StatType.Constitution, 2)
                }),
                new EquipmentMaster(4006, EquipmentSlot.Accessory, 0, new[]
                {
                    new StatBonus(StatType.Wisdom, 2),
                    new StatBonus(StatType.Intelligence, 1)
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
                new WeaponMaster(3012, WeaponType.Bow, GetWeaponTypeCombatMaster(WeaponType.Bow), 10, 0, 0),
                new WeaponMaster(3013, WeaponType.Dagger, GetWeaponTypeCombatMaster(WeaponType.Dagger), 6, 0, 0),
                new WeaponMaster(3014, WeaponType.Dagger, GetWeaponTypeCombatMaster(WeaponType.Dagger), 9, 0, 0)
            }.ToDictionary(x => x.ItemId);
        }

        static IReadOnlyDictionary<int, ActorLoadoutMaster> CreateActorLoadoutMasters()
        {
            return new[]
            {
                new ActorLoadoutMaster(
                    1,
                    3001,
                    3101,
                    Array.Empty<int>(),
                    new[] { new ItemStack(2001, 1) }),
                new ActorLoadoutMaster(
                    2,
                    3010,
                    3103,
                    new[] { 4006 },
                    new[] { new ItemStack(2001, 1) }),
                new ActorLoadoutMaster(
                    3,
                    3002,
                    3101,
                    Array.Empty<int>(),
                    new[] { new ItemStack(2001, 1) }),
                new ActorLoadoutMaster(
                    4,
                    3011,
                    3103,
                    new[] { 4006 },
                    new[] { new ItemStack(2001, 1), new ItemStack(2002, 1) }),
                new ActorLoadoutMaster(
                    5,
                    3013,
                    3101,
                    new[] { 4005 },
                    new[] { new ItemStack(2001, 1) })
            }.ToDictionary(x => x.Id);
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
                        new ActorDropEntry(1, 0.5f, 1, 4)
                    }),
                new SpeciesMaster(
                    2,
                    "Orc",
                    new[]
                    {
                        new ActorDropEntry(1, 0.7f, 3, 8),
                        new ActorDropEntry(3004, 0.18f, 1, 1)
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
                    4,
                    "Slime",
                    new[]
                    {
                        new ActorDropEntry(1003, 0.8f, 1, 2),
                        new ActorDropEntry(1, 0.4f, 1, 3)
                    }),
                new SpeciesMaster(
                    5,
                    "Wolf",
                    new[]
                    {
                        new ActorDropEntry(1004, 0.65f, 1, 1),
                        new ActorDropEntry(1, 0.5f, 2, 5)
                    }),
                new SpeciesMaster(
                    6,
                    "Skeleton",
                    new[]
                    {
                        new ActorDropEntry(1006, 0.65f, 1, 2),
                        new ActorDropEntry(1, 0.55f, 3, 7)
                    }),
                new SpeciesMaster(
                    7,
                    "Bat",
                    new[]
                    {
                        new ActorDropEntry(1005, 0.7f, 1, 2),
                        new ActorDropEntry(1, 0.45f, 2, 5)
                    }),
                new SpeciesMaster(
                    8,
                    "Golem",
                    new[]
                    {
                        new ActorDropEntry(1007, 0.4f, 1, 1),
                        new ActorDropEntry(1101, 0.7f, 2, 5),
                        new ActorDropEntry(1, 0.8f, 8, 15)
                    }),
                new SpeciesMaster(
                    9,
                    "Dragonkin",
                    new[]
                    {
                        new ActorDropEntry(1008, 0.35f, 1, 1),
                        new ActorDropEntry(1107, 0.25f, 1, 2),
                        new ActorDropEntry(1, 0.9f, 12, 24)
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
                    1),
                new ActorArchetypeMaster(
                    2,
                    "Goblin",
                    "monster_goblin",
                    ActorBehaviorType.Monster,
                    1,
                    WeaponType.Claws,
                    new ActorStats(3, 6, 3, 2, 2, 1),
                    1,
                    2),
                new ActorArchetypeMaster(
                    3,
                    "Orc",
                    "monster_orc",
                    ActorBehaviorType.Monster,
                    2,
                    WeaponType.Scythe,
                    new ActorStats(8, 7, 8, 4, 4, 3),
                    1,
                    2),
                new ActorArchetypeMaster(
                    4,
                    "Ogre",
                    "monster_ogre",
                    ActorBehaviorType.Monster,
                    3,
                    WeaponType.Fist,
                    new ActorStats(14, 8, 14, 6, 6, 8),
                    1,
                    2),
                new ActorArchetypeMaster(
                    5,
                    "Goblin Archer",
                    "monster_goblin_archer",
                    ActorBehaviorType.Monster,
                    1,
                    WeaponType.Bow,
                    new ActorStats(3, 9, 3, 2, 2, 1),
                    1,
                    2),
                new ActorArchetypeMaster(
                    11,
                    "Warrior Adventurer",
                    "adventurer_novice",
                    ActorBehaviorType.Adventurer,
                    10,
                    WeaponType.Sword,
                    new ActorStats(12, 8, 11, 5, 5, 7),
                    1,
                    1,
                    1),
                new ActorArchetypeMaster(
                    12,
                    "Mage Adventurer",
                    "adventurer_novice",
                    ActorBehaviorType.Adventurer,
                    10,
                    WeaponType.Staff,
                    new ActorStats(5, 7, 7, 13, 11, 7),
                    1,
                    1,
                    2),
                new ActorArchetypeMaster(
                    13,
                    "Archer Adventurer",
                    "adventurer_novice",
                    ActorBehaviorType.Adventurer,
                    10,
                    WeaponType.Bow,
                    new ActorStats(8, 13, 8, 6, 7, 6),
                    1,
                    1,
                    3),
                new ActorArchetypeMaster(
                    14,
                    "Healer Adventurer",
                    "adventurer_novice",
                    ActorBehaviorType.Adventurer,
                    10,
                    WeaponType.Staff,
                    new ActorStats(6, 7, 9, 8, 14, 8),
                    1,
                    1,
                    4),
                new ActorArchetypeMaster(
                    15,
                    "Scout Adventurer",
                    "adventurer_novice",
                    ActorBehaviorType.Adventurer,
                    10,
                    WeaponType.Dagger,
                    new ActorStats(8, 14, 8, 6, 7, 8),
                    1,
                    1,
                    5),
                new ActorArchetypeMaster(
                    101,
                    "Slime",
                    "monster_slime",
                    ActorBehaviorType.Monster,
                    4,
                    WeaponType.Fist,
                    new ActorStats(2, 3, 5, 1, 1, 1),
                    1,
                    2),
                new ActorArchetypeMaster(
                    102,
                    "Goblin",
                    "monster_goblin",
                    ActorBehaviorType.Monster,
                    1,
                    WeaponType.Claws,
                    new ActorStats(3, 6, 3, 2, 2, 1),
                    1,
                    2),
                new ActorArchetypeMaster(
                    103,
                    "Wolf",
                    "monster_wolf",
                    ActorBehaviorType.Monster,
                    5,
                    WeaponType.Fangs,
                    new ActorStats(5, 9, 5, 2, 2, 2),
                    1,
                    2),
                new ActorArchetypeMaster(
                    104,
                    "Skeleton",
                    "monster_skeleton",
                    ActorBehaviorType.Monster,
                    6,
                    WeaponType.Sword,
                    new ActorStats(7, 6, 7, 2, 2, 1),
                    1,
                    2),
                new ActorArchetypeMaster(
                    105,
                    "Bat",
                    "monster_bat",
                    ActorBehaviorType.Monster,
                    7,
                    WeaponType.Fangs,
                    new ActorStats(3, 11, 3, 2, 2, 1),
                    1,
                    2),
                new ActorArchetypeMaster(
                    106,
                    "Orc",
                    "monster_orc",
                    ActorBehaviorType.Monster,
                    2,
                    WeaponType.Scythe,
                    new ActorStats(8, 7, 8, 4, 4, 3),
                    1,
                    2),
                new ActorArchetypeMaster(
                    107,
                    "Golem",
                    "monster_golem",
                    ActorBehaviorType.Monster,
                    8,
                    WeaponType.Fist,
                    new ActorStats(14, 8, 14, 6, 6, 8),
                    1,
                    2),
                new ActorArchetypeMaster(
                    108,
                    "Dragonkin",
                    "monster_dragonkin",
                    ActorBehaviorType.Monster,
                    9,
                    WeaponType.Fangs,
                    new ActorStats(16, 10, 14, 9, 8, 9),
                    1,
                    2),
                new ActorArchetypeMaster(
                    109,
                    "Goblin Archer",
                    "monster_goblin_archer",
                    ActorBehaviorType.Monster,
                    1,
                    WeaponType.Bow,
                    new ActorStats(3, 9, 3, 2, 2, 1),
                    1,
                    2),
                new ActorArchetypeMaster(
                    201,
                    "Elite Orc",
                    "monster_orc",
                    ActorBehaviorType.Monster,
                    2,
                    WeaponType.Axe,
                    new ActorStats(12, 9, 12, 5, 5, 5),
                    4,
                    2),
                new ActorArchetypeMaster(
                    202,
                    "Boss Dragonkin",
                    "monster_dragonkin",
                    ActorBehaviorType.Monster,
                    9,
                    WeaponType.Fangs,
                    new ActorStats(22, 14, 20, 12, 10, 12),
                    8,
                    2)
            }.ToDictionary(x => x.Id);
        }

        static IReadOnlyDictionary<int, AdventurerSpawnMaster> CreateAdventurerSpawnMasters()
        {
            return new[]
            {
                new AdventurerSpawnMaster(1, "アリス", 1, true),
                new AdventurerSpawnMaster(2, "ヤスオ", 15, true),
                new AdventurerSpawnMaster(3, "アカリ", 13, true),
                new AdventurerSpawnMaster(4, "ゼド", 15, true),
                new AdventurerSpawnMaster(5, "ラックス", 12, true),
                new AdventurerSpawnMaster(6, "ガレン", 11, true),
                new AdventurerSpawnMaster(7, "セト", 11, true),
                new AdventurerSpawnMaster(8, "ユーミ", 14, true),
                new AdventurerSpawnMaster(9, "トリンダメア", 11, false),
                new AdventurerSpawnMaster(10, "マルザハール", 12, false),
                new AdventurerSpawnMaster(11, "アッシュ", 13, false),
                new AdventurerSpawnMaster(12, "ソラカ", 14, false),
                new AdventurerSpawnMaster(13, "クイン", 15, false),
                new AdventurerSpawnMaster(14, "ケイトリン", 15, false),
                new AdventurerSpawnMaster(15, "ウーコン", 15, false),
                new AdventurerSpawnMaster(16, "アーゴット", 15, false),
                new AdventurerSpawnMaster(17, "アーリ", 15, false),
                new AdventurerSpawnMaster(18, "アイバーン", 15, false),
                new AdventurerSpawnMaster(19, "ヨネ", 15, false),
                new AdventurerSpawnMaster(20, "イー", 15, false),
                new AdventurerSpawnMaster(21, "ケネン", 15, false),
                new AdventurerSpawnMaster(22, "スレッシュ", 15, false),
                new AdventurerSpawnMaster(23, "セジュアニ", 15, false),
                new AdventurerSpawnMaster(24, "タロン", 15, false),
                new AdventurerSpawnMaster(25, "ニーコ", 15, false),
                new AdventurerSpawnMaster(26, "ヨリック", 15, false),
                new AdventurerSpawnMaster(27, "ユナラ", 15, false),
                new AdventurerSpawnMaster(28, "サイオン", 15, false),
                new AdventurerSpawnMaster(29, "ヴァイ", 15, false),
                new AdventurerSpawnMaster(30, "ジンクス", 15, false),
                new AdventurerSpawnMaster(31, "ラカン", 15, false)
            }.ToDictionary(x => x.Id);
        }

        static IReadOnlyDictionary<int, SpawnTableMaster> CreateSpawnTableMasters()
        {
            var earlyAdventurerEntries = new[]
            {
                new SpawnTableEntryMaster(1, 1, 1, 100, 1, 1),
                new SpawnTableEntryMaster(6, 1, 6, 100, 1, 1),
                new SpawnTableEntryMaster(7, 1, 7, 100, 1, 1),
                new SpawnTableEntryMaster(9, 1, 9, 80, 1, 2),
                new SpawnTableEntryMaster(13, 1, 13, 65, 1, 2)
            };
            var middleAdventurerEntries = new[]
            {
                new SpawnTableEntryMaster(34, 7, 2, 80, 1, 1),
                new SpawnTableEntryMaster(35, 7, 3, 80, 1, 1),
                new SpawnTableEntryMaster(36, 7, 5, 80, 1, 1),
                new SpawnTableEntryMaster(37, 7, 10, 90, 1, 3),
                new SpawnTableEntryMaster(38, 7, 11, 100, 1, 3),
                new SpawnTableEntryMaster(39, 7, 12, 90, 1, 3),
                new SpawnTableEntryMaster(40, 7, 9, 60, 1, 3),
                new SpawnTableEntryMaster(41, 7, 13, 60, 1, 3)
            };
            var shallowMonsterEntries = new[]
            {
                new SpawnTableEntryMaster(14, 2, 101, 60, 1, 2),
                new SpawnTableEntryMaster(15, 2, 102, 35, 1, 3),
                new SpawnTableEntryMaster(16, 2, 103, 25, 1, 3),
                new SpawnTableEntryMaster(17, 2, 109, 15, 1, 3)
            };
            var middleMonsterEntries = new[]
            {
                new SpawnTableEntryMaster(18, 3, 102, 35, 3, 5),
                new SpawnTableEntryMaster(19, 3, 104, 30, 3, 6),
                new SpawnTableEntryMaster(20, 3, 105, 30, 3, 5),
                new SpawnTableEntryMaster(21, 3, 106, 20, 4, 7),
                new SpawnTableEntryMaster(22, 3, 109, 20, 3, 6)
            };
            var deepMonsterEntries = new[]
            {
                new SpawnTableEntryMaster(23, 4, 106, 35, 6, 9),
                new SpawnTableEntryMaster(24, 4, 107, 25, 7, 10),
                new SpawnTableEntryMaster(25, 4, 108, 18, 8, 11),
                new SpawnTableEntryMaster(26, 4, 201, 12, 8, 12)
            };
            var bossMonsterEntries = new[]
            {
                new SpawnTableEntryMaster(27, 5, 201, 65, 8, 12),
                new SpawnTableEntryMaster(28, 5, 202, 35, 10, 15)
            };
            var endlessMonsterEntries = new[]
            {
                new SpawnTableEntryMaster(29, 6, 106, 25, 10, 20),
                new SpawnTableEntryMaster(30, 6, 107, 25, 10, 20),
                new SpawnTableEntryMaster(31, 6, 108, 25, 10, 20),
                new SpawnTableEntryMaster(32, 6, 201, 15, 12, 24),
                new SpawnTableEntryMaster(33, 6, 202, 10, 15, 30)
            };

            return new[]
            {
                new SpawnTableMaster(1, "Early Adventurer Spawn", SpawnTableTargetType.AdventurerSpawn, earlyAdventurerEntries),
                new SpawnTableMaster(2, "Shallow Monster Spawn", SpawnTableTargetType.ActorArchetype, shallowMonsterEntries),
                new SpawnTableMaster(3, "Middle Monster Spawn", SpawnTableTargetType.ActorArchetype, middleMonsterEntries),
                new SpawnTableMaster(4, "Deep Monster Spawn", SpawnTableTargetType.ActorArchetype, deepMonsterEntries),
                new SpawnTableMaster(5, "Boss Monster Spawn", SpawnTableTargetType.ActorArchetype, bossMonsterEntries),
                new SpawnTableMaster(6, "Endless Monster Spawn", SpawnTableTargetType.ActorArchetype, endlessMonsterEntries),
                new SpawnTableMaster(7, "Middle Adventurer Spawn", SpawnTableTargetType.AdventurerSpawn, middleAdventurerEntries)
            }.ToDictionary(x => x.Id);
        }

        static IReadOnlyDictionary<int, AdventurerSpawnBandMaster> CreateAdventurerSpawnBandMasters()
        {
            return new[]
            {
                new AdventurerSpawnBandMaster(1, 0, 1, 10),
                new AdventurerSpawnBandMaster(2, 3, 7, 20)
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

            foreach (var loadoutMaster in actorLoadoutMasters.Values)
            {
                ValidateOptionalEquipment(loadoutMaster.WeaponItemId, EquipmentSlot.Weapon);
                ValidateOptionalEquipment(loadoutMaster.ArmorItemId, EquipmentSlot.Armor);
                foreach (var accessoryItemId in loadoutMaster.AccessoryItemIds)
                {
                    ValidateOptionalEquipment(accessoryItemId, EquipmentSlot.Accessory);
                }

                ValidateItemStacks(loadoutMaster.InitialInventory);
            }

            foreach (var archetypeMaster in actorArchetypeMasters.Values)
            {
                GetSpeciesMaster(archetypeMaster.SpeciesId);
                GetWeaponTypeCombatMaster(archetypeMaster.DefaultWeaponType);
                GetLevelTable(archetypeMaster.LevelTableId);
                GetActorVisualMaster(archetypeMaster.VisualId, GameConstants.DefaultActorSkinId);
                if (0 < archetypeMaster.LoadoutMasterId)
                {
                    GetActorLoadoutMaster(archetypeMaster.LoadoutMasterId);
                }
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

            foreach (var spawnBandMaster in adventurerSpawnBandMasters.Values)
            {
                var spawnTableMaster = GetSpawnTableMaster(spawnBandMaster.SpawnTableId);
                if (spawnTableMaster.TargetType != SpawnTableTargetType.AdventurerSpawn)
                {
                    throw new InvalidOperationException("Adventurer spawn band requires adventurer spawn table.");
                }
            }

            foreach (var depthBandMaster in dungeonDepthBandMasters.Values)
            {
                var spawnTableMaster = GetSpawnTableMaster(depthBandMaster.MonsterSpawnTableId);
                if (spawnTableMaster.TargetType != SpawnTableTargetType.ActorArchetype)
                {
                    throw new InvalidOperationException("Dungeon depth band requires actor archetype spawn table.");
                }
            }

            foreach (var lineupItemMaster in facilityLineupItemMasters.Values)
            {
                if (!facilityLineupMasters.ContainsKey(lineupItemMaster.LineupId))
                {
                    throw new InvalidOperationException("Facility lineup item references unknown lineup.");
                }

                RequireItem(lineupItemMaster.ItemId);
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

        void ValidateOptionalEquipment(int itemId, EquipmentSlot expectedSlot)
        {
            if (itemId == 0)
            {
                return;
            }

            var equipmentMaster = GetEquipmentMaster(itemId);
            if (equipmentMaster.Slot != expectedSlot)
            {
                throw new InvalidOperationException("Actor loadout equipment slot does not match expected slot.");
            }

            if (expectedSlot == EquipmentSlot.Weapon)
            {
                GetWeaponMaster(itemId);
            }
        }

        static string CreateActorVisualKey(string visualId, int skinId)
        {
            return $"{visualId}:{skinId}";
        }
    }
}
