using System;

namespace DungeonInn.Master
{
    public sealed class SpawnTableEntryMaster
    {
        public int Id { get; }
        public int SpawnTableId { get; }
        public int TargetId { get; }
        public int Weight { get; }
        public int MinLevel { get; }
        public int MaxLevel { get; }

        public SpawnTableEntryMaster(
            int id,
            int spawnTableId,
            int targetId,
            int weight,
            int minLevel,
            int maxLevel)
        {
            if (id < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            if (spawnTableId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(spawnTableId));
            }

            if (targetId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(targetId));
            }

            if (weight < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(weight));
            }

            if (minLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(minLevel));
            }

            if (maxLevel < minLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(maxLevel));
            }

            Id = id;
            SpawnTableId = spawnTableId;
            TargetId = targetId;
            Weight = weight;
            MinLevel = minLevel;
            MaxLevel = maxLevel;
        }
    }
}
