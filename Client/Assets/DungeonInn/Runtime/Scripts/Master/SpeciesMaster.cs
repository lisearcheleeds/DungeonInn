using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Domain.Item;

namespace DungeonInn.Master
{
    public sealed class SpeciesMaster
    {
        public int Id { get; }
        public string Name { get; }
        public IReadOnlyList<ActorDropEntry> SpeciesDrops { get; }

        public SpeciesMaster(
            int id,
            string name,
            IReadOnlyList<ActorDropEntry> speciesDrops)
        {
            if (id < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Species name is required.", nameof(name));
            }

            Id = id;
            Name = name;
            SpeciesDrops = (speciesDrops ?? Array.Empty<ActorDropEntry>()).ToArray();
        }
    }
}
