using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonInn.Master
{
    public sealed class SpawnTableMaster
    {
        public int Id { get; }
        public string Name { get; }
        public SpawnTableTargetType TargetType { get; }
        public IReadOnlyList<SpawnTableEntryMaster> Entries { get; }

        public SpawnTableMaster(
            int id,
            string name,
            SpawnTableTargetType targetType,
            IReadOnlyList<SpawnTableEntryMaster> entries)
        {
            if (id < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Spawn table name is required.", nameof(name));
            }

            if (entries == null || entries.Count == 0)
            {
                throw new ArgumentException("Spawn table requires entries.", nameof(entries));
            }

            Id = id;
            Name = name;
            TargetType = targetType;
            Entries = entries.ToArray();
        }
    }
}
