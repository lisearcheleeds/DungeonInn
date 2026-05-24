using System;

namespace DungeonInn.Application.World
{
    public sealed class SpawnBalanceSettings
    {
        public int AdventurerSpawnIntervalTicks { get; }
        public int MonsterSpawnIntervalTicks { get; }
        public int MaxAdventurerCount { get; }
        public int MaxMonsterCount { get; }

        public SpawnBalanceSettings(
            int adventurerSpawnIntervalTicks,
            int monsterSpawnIntervalTicks,
            int maxAdventurerCount,
            int maxMonsterCount)
        {
            AdventurerSpawnIntervalTicks = Math.Max(1, adventurerSpawnIntervalTicks);
            MonsterSpawnIntervalTicks = Math.Max(1, monsterSpawnIntervalTicks);
            MaxAdventurerCount = Math.Max(0, maxAdventurerCount);
            MaxMonsterCount = Math.Max(0, maxMonsterCount);
        }

        public static SpawnBalanceSettings CreateDefault()
        {
            return new SpawnBalanceSettings(5, 10, 8, 20);
        }
    }
}
