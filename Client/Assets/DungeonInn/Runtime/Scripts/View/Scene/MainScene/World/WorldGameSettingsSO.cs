using DungeonInn.Application.World;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Item;
using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    [CreateAssetMenu(menuName = "DungeonInn/World/WorldGameSettings")]
    public sealed class WorldGameSettingsSO : ScriptableObject
    {
        [Header("Ground Map Generation")]
        [SerializeField] int groundMapWidth = 30;
        [SerializeField] int groundMapDepth = 30;
        [SerializeField] int groundFacilityBuildingSizeCells = 5;
        [SerializeField] int groundFacilityBuildingInnerOffsetCells = 7;
        [SerializeField] int groundFacilityBuildingOuterOffsetCells = 2;

        [Header("World Map View")]
        [SerializeField] int mapChunkTileSize = 16;
        [SerializeField] int mapChunkBuildsPerFrame = 1;
        [SerializeField] float mapTileHeightMeters = 2f;

        [Header("Dungeon Map Generation")]
        [SerializeField] int dungeonFloorWidth = 50;
        [SerializeField] int dungeonFloorDepth = 50;
        [SerializeField] int dungeonThemeFloorsPerTheme = 10;
        [SerializeField] int dungeonSectionSizeCells = 25;
        [SerializeField] int dungeonSectionMarginCells = 2;
        [SerializeField] int dungeonPathDistortBaseStrength = 8;
        [SerializeField] int dungeonExtraReferencePointMaxCount = 4;
        [SerializeField] int dungeonRoomMinSizeCells = 3;
        [SerializeField] int dungeonRoomMaxSizeCells = 5;

        [Header("Initial World")]
        [SerializeField] int initialDungeonSeed = 12345;
        [SerializeField] int initialGameRandomSeed = 42195;
        [SerializeField] int initialGuildGold = 10000;
        [SerializeField] int initialGeneralStoreGold = 1000;
        [SerializeField] int initialEquipmentShopGold = 1000;
        [SerializeField] int initialInnReputation = 10;
        [SerializeField] int initialGuildInventorySlotCapacity = 100;

        [Header("Initial Facilities")]
        [SerializeField] int initialInnBasePrice = 10;
        [SerializeField] int initialInnCapacity = 8;
        [SerializeField] int initialGeneralStoreBasePrice = 10;
        [SerializeField] int initialEquipmentShopBasePrice = 10;
        [SerializeField] int initialShopCapacity = 1;

        [Header("Initial Guild Inventory")]
        [SerializeField] int initialRookieSwordItemId = 3001;
        [SerializeField] int initialRookieSwordCount = 20;
        [SerializeField] int initialRookieArmorItemId = 3003;
        [SerializeField] int initialRookieArmorCount = 20;

        [Header("Inn Balance")]
        [SerializeField] float innHpRecoveryPercentPerMinute = 1.0f;
        [SerializeField] int innFeePerStay = 10;
        [SerializeField] int adventurerInnWaitDepartureDays = 3;
        [SerializeField] int innStayedSatisfactionDelta = 2;
        [SerializeField] int innWaitingSatisfactionDelta = -2;
        [SerializeField] int innCannotPaySatisfactionDelta = -1;

        [Header("Actor Simulation")]
        [SerializeField] float actorMoveSpeedMetersPerSecond = 5f;
        [SerializeField] float actorMoveArrivalDistanceMeters = 2.5f;
        [SerializeField] float adventurerItemPickupRadiusMeters = 1.5f;
        [SerializeField] int adventurerExplorationRoomArrivalTarget = 8;

        [Header("Spawn Balance")]
        [SerializeField] int adventurerSpawnIntervalTicks = 5;
        [SerializeField] int monsterSpawnIntervalTicks = 10;
        [SerializeField] int initialMaxAdventurerCount = 8;
        [SerializeField] int initialMaxMonsterCount = 20;

        [Header("Adventurer Return Policy")]
        [SerializeField] int adventurerReturnDecisionThresholdScore = 100;
        [SerializeField] int adventurerReturnGoalCompletedScore = 100;
        [SerializeField] int adventurerReturnCriticalHpScore = 100;
        [SerializeField] int adventurerReturnLowHpWithoutRecoveryItemScore = 100;
        [SerializeField] float adventurerReturnLowHpRatio = 0.6f;
        [SerializeField] float adventurerReturnCriticalHpRatio = 0.3f;

        [Header("Combat Balance")]
        [SerializeField] float projectileHitRadiusMeters = 0.5f;
        [SerializeField] float combatEncounterRangeMeters = 20f;
        [SerializeField] float actorSpatialIndexCellSizeMeters = 20f;

        public GroundMapGenerationSettings ToGroundMapGenerationSettings()
        {
            return new GroundMapGenerationSettings(
                groundMapWidth,
                groundMapDepth,
                groundFacilityBuildingSizeCells,
                groundFacilityBuildingInnerOffsetCells,
                groundFacilityBuildingOuterOffsetCells);
        }

        public WorldMapViewSettings ToWorldMapViewSettings()
        {
            return new WorldMapViewSettings(
                mapChunkTileSize,
                mapChunkBuildsPerFrame,
                mapTileHeightMeters);
        }

        public DungeonMapGenerationSettings ToDungeonMapGenerationSettings()
        {
            return new DungeonMapGenerationSettings(
                dungeonFloorWidth,
                dungeonFloorDepth,
                dungeonThemeFloorsPerTheme,
                dungeonSectionSizeCells,
                dungeonSectionMarginCells,
                dungeonPathDistortBaseStrength,
                dungeonExtraReferencePointMaxCount,
                dungeonRoomMinSizeCells,
                dungeonRoomMaxSizeCells);
        }

        public InitialWorldSettings ToInitialWorldSettings()
        {
            var initialGuildReserveGold = initialGuildGold - initialGeneralStoreGold - initialEquipmentShopGold;
            return new InitialWorldSettings(
                initialDungeonSeed,
                initialGameRandomSeed,
                initialGuildGold,
                initialGeneralStoreGold,
                initialEquipmentShopGold,
                initialInnReputation,
                initialGuildInventorySlotCapacity,
                initialInnBasePrice,
                initialInnCapacity,
                initialGeneralStoreBasePrice,
                initialEquipmentShopBasePrice,
                initialShopCapacity,
                initialRookieSwordItemId,
                initialRookieArmorItemId,
                new[]
                {
                    new ItemStack(SpecialItemIds.Money, initialGuildReserveGold),
                    new ItemStack(initialRookieSwordItemId, initialRookieSwordCount),
                    new ItemStack(initialRookieArmorItemId, initialRookieArmorCount)
                });
        }

        public InnBalanceSettings ToInnBalanceSettings()
        {
            return new InnBalanceSettings(
                innHpRecoveryPercentPerMinute,
                innFeePerStay,
                adventurerInnWaitDepartureDays,
                innStayedSatisfactionDelta,
                innWaitingSatisfactionDelta,
                innCannotPaySatisfactionDelta);
        }

        public ActorSimulationSettings ToActorSimulationSettings()
        {
            return new ActorSimulationSettings(
                actorMoveSpeedMetersPerSecond,
                actorMoveArrivalDistanceMeters,
                adventurerItemPickupRadiusMeters,
                adventurerExplorationRoomArrivalTarget);
        }

        public SpawnBalanceSettings ToSpawnBalanceSettings()
        {
            return new SpawnBalanceSettings(
                adventurerSpawnIntervalTicks,
                monsterSpawnIntervalTicks,
                initialMaxAdventurerCount,
                initialMaxMonsterCount);
        }

        public AdventurerReturnPolicySettings ToAdventurerReturnPolicySettings()
        {
            return new AdventurerReturnPolicySettings(
                adventurerReturnDecisionThresholdScore,
                adventurerReturnGoalCompletedScore,
                adventurerReturnCriticalHpScore,
                adventurerReturnLowHpWithoutRecoveryItemScore,
                adventurerReturnLowHpRatio,
                adventurerReturnCriticalHpRatio);
        }

        public CombatBalanceSettings ToCombatBalanceSettings()
        {
            return new CombatBalanceSettings(
                projectileHitRadiusMeters,
                combatEncounterRangeMeters,
                actorSpatialIndexCellSizeMeters);
        }
    }
}
