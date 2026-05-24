using System;

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
}
