using System;

namespace DungeonInn.Application.World
{
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
}
