using System;

namespace DungeonInn.Domain.Dungeon
{
    public sealed class DungeonDepthBandConfig
    {
        public int MinFloorIndex { get; }
        public int MaxFloorIndex { get; }
        public int MonsterTableId { get; }
        public int MonsterLevel { get; }
        public DungeonFloorGenerationSettings GenerationSettings { get; }

        public DungeonDepthBandConfig(
            int minFloorIndex,
            int maxFloorIndex,
            int monsterTableId,
            int monsterLevel,
            DungeonFloorGenerationSettings generationSettings)
        {
            if (minFloorIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(minFloorIndex));
            }

            if (maxFloorIndex < minFloorIndex)
            {
                throw new ArgumentOutOfRangeException(nameof(maxFloorIndex));
            }

            if (monsterLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(monsterLevel));
            }

            MinFloorIndex = minFloorIndex;
            MaxFloorIndex = maxFloorIndex;
            MonsterTableId = Math.Max(0, monsterTableId);
            MonsterLevel = monsterLevel;
            GenerationSettings = generationSettings ?? throw new ArgumentNullException(nameof(generationSettings));
        }

        public bool Contains(int floorIndex)
        {
            return MinFloorIndex <= floorIndex && floorIndex <= MaxFloorIndex;
        }
    }
}
