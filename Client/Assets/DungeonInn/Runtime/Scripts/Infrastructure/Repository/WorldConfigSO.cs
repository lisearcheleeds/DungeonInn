using DungeonInn.Domain.World;
using UnityEngine;

namespace DungeonInn.Infrastructure.Repository
{
    [CreateAssetMenu(menuName = "DungeonInn/Config/WorldConfig")]
    public class WorldConfigSO : ScriptableObject
    {
        public int SizeX = 1000;
        public int SizeY = 1000;
        public int SizeZ = 1000;
        public int DungeonFloorCount = 5;
        public int DungeonFloorHeight = 20;

        public WorldConfigData ToData()
        {
            return new WorldConfigData
            {
                SizeX = SizeX,
                SizeY = SizeY,
                SizeZ = SizeZ,
                DungeonFloorCount = DungeonFloorCount,
                DungeonFloorHeight = DungeonFloorHeight,
            };
        }
    }
}
