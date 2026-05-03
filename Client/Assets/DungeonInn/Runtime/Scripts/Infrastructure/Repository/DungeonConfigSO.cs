using DungeonInn.Domain.Dungeon;
using UnityEngine;

namespace DungeonInn.Infrastructure.Repository
{
    [CreateAssetMenu(menuName = "DungeonInn/Config/DungeonConfig")]
    public class DungeonConfigSO : ScriptableObject
    {
        public int Seed = 0;
        public int MinStairsCount = 1;
        public string GeneratorType = "Maze";
        public int FloorSizeX = 50;
        public int FloorSizeZ = 50;

        public DungeonConfigData ToData()
        {
            return new DungeonConfigData
            {
                Seed = Seed,
                MinStairsCount = MinStairsCount,
                GeneratorType = GeneratorType,
                FloorSizeX = FloorSizeX,
                FloorSizeZ = FloorSizeZ,
            };
        }
    }
}
