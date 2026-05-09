using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Item;

namespace DungeonInn.Master
{
    public sealed class MonsterSpeciesMaster
    {
        public int Id { get; }
        public string Name { get; }
        public int ActorArchetypeId { get; }
        public WeaponType DefaultWeaponType { get; }
        public bool CanScavenge { get; }
        public IReadOnlyList<ActorDropEntry> SpeciesDrops { get; }

        public MonsterSpeciesMaster(
            int id,
            string name,
            int actorArchetypeId,
            WeaponType defaultWeaponType,
            bool canScavenge,
            IReadOnlyList<ActorDropEntry> speciesDrops)
        {
            if (id < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Monster species name is required.", nameof(name));
            }

            if (actorArchetypeId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(actorArchetypeId));
            }

            if (defaultWeaponType == WeaponType.None)
            {
                throw new ArgumentException("Default weapon type is required.", nameof(defaultWeaponType));
            }

            Id = id;
            Name = name;
            ActorArchetypeId = actorArchetypeId;
            DefaultWeaponType = defaultWeaponType;
            CanScavenge = canScavenge;
            SpeciesDrops = (speciesDrops ?? Array.Empty<ActorDropEntry>()).ToArray();
        }
    }
}
