using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonInn.Application.Dungeons
{
    public sealed class DungeonLayerInfoSummary
    {
        public DungeonLayerInfoSummary(
            int floorIndex,
            bool isGenerated,
            int adventurerCount,
            int monsterCount,
            float difficultyCoefficient,
            IReadOnlyList<DungeonLayerMonsterSpawnSummary> monsterSpawns,
            IReadOnlyList<DungeonLayerItemDropSummary> itemDrops)
        {
            if (floorIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(floorIndex));
            }

            FloorIndex = floorIndex;
            IsGenerated = isGenerated;
            AdventurerCount = Math.Max(0, adventurerCount);
            MonsterCount = Math.Max(0, monsterCount);
            DifficultyCoefficient = Math.Max(0f, difficultyCoefficient);
            MonsterSpawns = (monsterSpawns ?? Array.Empty<DungeonLayerMonsterSpawnSummary>()).ToArray();
            ItemDrops = (itemDrops ?? Array.Empty<DungeonLayerItemDropSummary>()).ToArray();
        }

        public int FloorIndex { get; }
        public bool IsGenerated { get; }
        public int AdventurerCount { get; }
        public int MonsterCount { get; }
        public float DifficultyCoefficient { get; }
        public IReadOnlyList<DungeonLayerMonsterSpawnSummary> MonsterSpawns { get; }
        public IReadOnlyList<DungeonLayerItemDropSummary> ItemDrops { get; }
    }
}
