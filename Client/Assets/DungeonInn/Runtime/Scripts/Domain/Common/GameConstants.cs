namespace DungeonInn.Domain.Common
{
    public static class GameConstants
    {
        public const int GameScheduleTicksPerDay = 1200;
        public const int InitialDungeonSeed = 12345;
        public const int InitialGuildGold = 10000;
        public const int InitialRookieSwordCount = 20;
        public const int InitialRookieArmorCount = 20;
        public const int InitialInnBasePrice = 10;
        public const int InitialInnCapacity = 8;

        public const float MapCellSizeMeters = 5f;

        public const int GroundMapWidth = 100;
        public const int GroundMapDepth = 100;
        public const int GroundFacilityBuildingSizeCells = 5;
        public const int GroundFacilityBuildingInnerOffsetCells = 7;
        public const int GroundFacilityBuildingOuterOffsetCells = 2;

        public const int DungeonFloorWidth = 200;
        public const int DungeonFloorDepth = 200;
        public const int DungeonStairPlacementMaxAttempts = 100;
        public const int DungeonFloorSeedMultiplier = 7919;
        public const int DungeonDefaultRoomCountBase = 4;
        public const int DungeonRoomCountFloorsPerIncrement = 5;
        public const int DungeonDefaultMinCorridorLength = 4;
        public const int DungeonDefaultMaxCorridorLengthBase = 12;
        public const int DungeonMaxCorridorLengthFloorsPerIncrement = 10;
        public const int DungeonThemeFloorsPerTheme = 10;
        public const int DungeonSectionSizeCells = 20;
        public const int DungeonSectionMarginCells = 2;
        public const int DungeonPathDistortBaseStrength = 4;
        public const int DungeonExtraReferencePointMaxCount = 2;
        public const int DungeonRoomMinSizeCells = 5;
        public const int DungeonRoomMaxSizeCells = 11;
    }
}
