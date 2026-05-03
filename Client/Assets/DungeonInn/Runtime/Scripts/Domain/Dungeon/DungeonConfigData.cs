namespace DungeonInn.Domain.Dungeon
{
    public class DungeonConfigData
    {
        public int Seed { get; init; }
        public int MinStairsCount { get; init; }
        public string GeneratorType { get; init; }
        public int FloorSizeX { get; init; }
        public int FloorSizeZ { get; init; }
    }
}
