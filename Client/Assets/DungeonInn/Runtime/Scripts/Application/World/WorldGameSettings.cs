using System;
using System.Collections.Generic;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Item;

namespace DungeonInn.Application.World
{
    public sealed class GroundMapGenerationSettings
    {
        public int Width { get; }
        public int Depth { get; }
        public int FacilityBuildingSizeCells { get; }
        public int FacilityBuildingInnerOffsetCells { get; }
        public int FacilityBuildingOuterOffsetCells { get; }

        public GroundMapGenerationSettings(
            int width,
            int depth,
            int facilityBuildingSizeCells,
            int facilityBuildingInnerOffsetCells,
            int facilityBuildingOuterOffsetCells)
        {
            Width = Math.Max(1, width);
            Depth = Math.Max(1, depth);
            FacilityBuildingSizeCells = Math.Max(1, facilityBuildingSizeCells);
            FacilityBuildingInnerOffsetCells = Math.Max(0, facilityBuildingInnerOffsetCells);
            FacilityBuildingOuterOffsetCells = Math.Max(0, facilityBuildingOuterOffsetCells);
        }

        public static GroundMapGenerationSettings CreateDefault()
        {
            return new GroundMapGenerationSettings(30, 30, 5, 7, 2);
        }
    }

    public sealed class DungeonMapGenerationSettings
    {
        public int FloorWidth { get; }
        public int FloorDepth { get; }
        public int ThemeFloorsPerTheme { get; }
        public int SectionSizeCells { get; }
        public int SectionMarginCells { get; }
        public int PathDistortBaseStrength { get; }
        public int ExtraReferencePointMaxCount { get; }
        public int RoomMinSizeCells { get; }
        public int RoomMaxSizeCells { get; }

        public DungeonMapGenerationSettings(
            int floorWidth,
            int floorDepth,
            int themeFloorsPerTheme,
            int sectionSizeCells,
            int sectionMarginCells,
            int pathDistortBaseStrength,
            int extraReferencePointMaxCount,
            int roomMinSizeCells,
            int roomMaxSizeCells)
        {
            FloorWidth = Math.Max(1, floorWidth);
            FloorDepth = Math.Max(1, floorDepth);
            ThemeFloorsPerTheme = Math.Max(1, themeFloorsPerTheme);
            SectionSizeCells = Math.Max(1, sectionSizeCells);
            SectionMarginCells = Math.Max(0, sectionMarginCells);
            PathDistortBaseStrength = Math.Max(0, pathDistortBaseStrength);
            ExtraReferencePointMaxCount = Math.Max(0, extraReferencePointMaxCount);
            RoomMinSizeCells = Math.Max(1, roomMinSizeCells);
            RoomMaxSizeCells = Math.Max(RoomMinSizeCells, roomMaxSizeCells);
        }

        public static DungeonMapGenerationSettings CreateDefault()
        {
            return new DungeonMapGenerationSettings(50, 50, 10, 25, 2, 8, 4, 3, 5);
        }
    }

    public sealed class InitialWorldSettings
    {
        public int DungeonSeed { get; }
        public int GameRandomSeed { get; }
        public int GuildGold { get; }
        public int GeneralStoreGold { get; }
        public int EquipmentShopGold { get; }
        public int InnReputation { get; }
        public int GuildInventorySlotCapacity { get; }
        public int InnBasePrice { get; }
        public int InnCapacity { get; }
        public int GeneralStoreBasePrice { get; }
        public int EquipmentShopBasePrice { get; }
        public int ShopCapacity { get; }
        public int InitialRookieSwordItemId { get; }
        public int InitialRookieArmorItemId { get; }
        public IReadOnlyList<ItemStack> InitialGuildInventory { get; }

        public int GuildReserveGold => GuildGold - GeneralStoreGold - EquipmentShopGold;

        public InitialWorldSettings(
            int dungeonSeed,
            int gameRandomSeed,
            int guildGold,
            int generalStoreGold,
            int equipmentShopGold,
            int innReputation,
            int guildInventorySlotCapacity,
            int innBasePrice,
            int innCapacity,
            int generalStoreBasePrice,
            int equipmentShopBasePrice,
            int shopCapacity,
            int initialRookieSwordItemId,
            int initialRookieArmorItemId,
            IReadOnlyList<ItemStack> initialGuildInventory)
        {
            DungeonSeed = dungeonSeed;
            GameRandomSeed = gameRandomSeed;
            GuildGold = Math.Max(0, guildGold);
            GeneralStoreGold = Math.Max(0, generalStoreGold);
            EquipmentShopGold = Math.Max(0, equipmentShopGold);
            InnReputation = Math.Max(0, innReputation);
            GuildInventorySlotCapacity = Math.Max(1, guildInventorySlotCapacity);
            InnBasePrice = Math.Max(0, innBasePrice);
            InnCapacity = Math.Max(1, innCapacity);
            GeneralStoreBasePrice = Math.Max(0, generalStoreBasePrice);
            EquipmentShopBasePrice = Math.Max(0, equipmentShopBasePrice);
            ShopCapacity = Math.Max(1, shopCapacity);
            InitialRookieSwordItemId = Math.Max(1, initialRookieSwordItemId);
            InitialRookieArmorItemId = Math.Max(1, initialRookieArmorItemId);
            InitialGuildInventory = initialGuildInventory ?? throw new ArgumentNullException(nameof(initialGuildInventory));
        }

        public static InitialWorldSettings CreateDefault()
        {
            return new InitialWorldSettings(
                12345,
                42195,
                10000,
                1000,
                1000,
                10,
                100,
                10,
                8,
                10,
                10,
                1,
                3001,
                3003,
                new[]
                {
                    new ItemStack(SpecialItemIds.Money, 8000),
                    new ItemStack(3001, 20),
                    new ItemStack(3003, 20)
                });
        }
    }

    public sealed class InnBalanceSettings
    {
        public float HpRecoveryPercentPerMinute { get; }
        public int FeePerStay { get; }
        public int AdventurerWaitDepartureDays { get; }
        public int StayedSatisfactionDelta { get; }
        public int WaitingSatisfactionDelta { get; }
        public int CannotPaySatisfactionDelta { get; }

        public InnBalanceSettings(
            float hpRecoveryPercentPerMinute,
            int feePerStay,
            int adventurerWaitDepartureDays,
            int stayedSatisfactionDelta,
            int waitingSatisfactionDelta,
            int cannotPaySatisfactionDelta)
        {
            HpRecoveryPercentPerMinute = Math.Max(0f, hpRecoveryPercentPerMinute);
            FeePerStay = Math.Max(0, feePerStay);
            AdventurerWaitDepartureDays = Math.Max(0, adventurerWaitDepartureDays);
            StayedSatisfactionDelta = stayedSatisfactionDelta;
            WaitingSatisfactionDelta = waitingSatisfactionDelta;
            CannotPaySatisfactionDelta = cannotPaySatisfactionDelta;
        }

        public static InnBalanceSettings CreateDefault()
        {
            return new InnBalanceSettings(1.0f, 10, 3, 2, -2, -1);
        }
    }

    public sealed class ActorSimulationSettings
    {
        public float MoveSpeedMetersPerSecond { get; }
        public float MoveArrivalDistanceMeters { get; }
        public float ItemPickupRadiusMeters { get; }
        public int ExplorationRoomArrivalTarget { get; }

        public ActorSimulationSettings(
            float moveSpeedMetersPerSecond,
            float moveArrivalDistanceMeters,
            float itemPickupRadiusMeters,
            int explorationRoomArrivalTarget)
        {
            MoveSpeedMetersPerSecond = Math.Max(0f, moveSpeedMetersPerSecond);
            MoveArrivalDistanceMeters = Math.Max(0f, moveArrivalDistanceMeters);
            ItemPickupRadiusMeters = Math.Max(0f, itemPickupRadiusMeters);
            ExplorationRoomArrivalTarget = Math.Max(1, explorationRoomArrivalTarget);
        }

        public static ActorSimulationSettings CreateDefault()
        {
            return new ActorSimulationSettings(5f, 2.5f, 1.5f, 8);
        }
    }

    public sealed class SpawnBalanceSettings
    {
        public int AdventurerSpawnIntervalTicks { get; }
        public int MonsterSpawnIntervalTicks { get; }
        public int MaxAdventurerCount { get; }
        public int MaxMonsterCount { get; }

        public SpawnBalanceSettings(
            int adventurerSpawnIntervalTicks,
            int monsterSpawnIntervalTicks,
            int maxAdventurerCount,
            int maxMonsterCount)
        {
            AdventurerSpawnIntervalTicks = Math.Max(1, adventurerSpawnIntervalTicks);
            MonsterSpawnIntervalTicks = Math.Max(1, monsterSpawnIntervalTicks);
            MaxAdventurerCount = Math.Max(0, maxAdventurerCount);
            MaxMonsterCount = Math.Max(0, maxMonsterCount);
        }

        public static SpawnBalanceSettings CreateDefault()
        {
            return new SpawnBalanceSettings(5, 10, 8, 20);
        }
    }

    public sealed class AdventurerReturnPolicySettings
    {
        public int DecisionThresholdScore { get; }
        public int GoalCompletedScore { get; }
        public int CriticalHpScore { get; }
        public int LowHpWithoutRecoveryItemScore { get; }
        public float LowHpRatio { get; }
        public float CriticalHpRatio { get; }

        public AdventurerReturnPolicySettings(
            int decisionThresholdScore,
            int goalCompletedScore,
            int criticalHpScore,
            int lowHpWithoutRecoveryItemScore,
            float lowHpRatio,
            float criticalHpRatio)
        {
            DecisionThresholdScore = Math.Max(0, decisionThresholdScore);
            GoalCompletedScore = Math.Max(0, goalCompletedScore);
            CriticalHpScore = Math.Max(0, criticalHpScore);
            LowHpWithoutRecoveryItemScore = Math.Max(0, lowHpWithoutRecoveryItemScore);
            LowHpRatio = Math.Max(0f, lowHpRatio);
            CriticalHpRatio = Math.Max(0f, criticalHpRatio);
        }

        public static AdventurerReturnPolicySettings CreateDefault()
        {
            return new AdventurerReturnPolicySettings(100, 100, 100, 100, 0.6f, 0.3f);
        }
    }

    public sealed class CombatBalanceSettings
    {
        public float ProjectileHitRadiusMeters { get; }
        public float EncounterRangeMeters { get; }
        public float SpatialIndexCellSizeMeters { get; }

        public CombatBalanceSettings(
            float projectileHitRadiusMeters,
            float encounterRangeMeters,
            float spatialIndexCellSizeMeters)
        {
            ProjectileHitRadiusMeters = Math.Max(0f, projectileHitRadiusMeters);
            EncounterRangeMeters = Math.Max(0f, encounterRangeMeters);
            SpatialIndexCellSizeMeters = Math.Max(0.01f, spatialIndexCellSizeMeters);
        }

        public static CombatBalanceSettings CreateDefault()
        {
            return new CombatBalanceSettings(0.5f, 20f, 20f);
        }
    }
}
