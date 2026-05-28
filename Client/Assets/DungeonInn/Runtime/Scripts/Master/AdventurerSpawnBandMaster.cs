using System;

namespace DungeonInn.Master
{
    public sealed class AdventurerSpawnBandMaster
    {
        public int Id { get; }
        public int MinDay { get; }
        public int SpawnTableId { get; }
        public int Priority { get; }

        public AdventurerSpawnBandMaster(int id, int minDay, int spawnTableId, int priority)
        {
            if (id < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            if (minDay < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minDay));
            }

            if (spawnTableId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(spawnTableId));
            }

            if (priority < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(priority));
            }

            Id = id;
            MinDay = minDay;
            SpawnTableId = spawnTableId;
            Priority = priority;
        }
    }
}
