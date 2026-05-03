namespace DungeonInn.Domain.Dungeon
{
    public interface IDungeonGenerator
    {
        DungeonMap Generate(int sizeX, int sizeZ, int floorCount, int floorHeight, int seed, int minStairs);
    }
}
