using System.Collections.Generic;

namespace DungeonInn.Application.GameLoop
{
    public sealed class SpawnScheduleState
    {
        readonly HashSet<int> spawnedAdventurerSpawnIds = new();

        public int LastAdventurerSpawnTick { get; set; } = -1;
        public int LastMonsterSpawnTick { get; set; } = -1;

        public bool HasSpawnedAdventurerSpawn(int adventurerSpawnId)
        {
            return spawnedAdventurerSpawnIds.Contains(adventurerSpawnId);
        }

        public void MarkAdventurerSpawned(int adventurerSpawnId)
        {
            spawnedAdventurerSpawnIds.Add(adventurerSpawnId);
        }
    }
}
