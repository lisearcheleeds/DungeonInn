using System;

namespace DungeonInn.Master
{
    public sealed class DungeonFloorExplorationMaster
    {
        public int FloorIndex { get; }
        public int MonsterSpawnTableId { get; }
        public float DifficultyCoefficient { get; }

        public DungeonFloorExplorationMaster(
            int floorIndex,
            int monsterSpawnTableId,
            float difficultyCoefficient)
        {
            if (floorIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(floorIndex));
            }

            if (monsterSpawnTableId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(monsterSpawnTableId));
            }

            if (difficultyCoefficient < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(difficultyCoefficient));
            }

            FloorIndex = floorIndex;
            MonsterSpawnTableId = monsterSpawnTableId;
            DifficultyCoefficient = difficultyCoefficient;
        }
    }
}
