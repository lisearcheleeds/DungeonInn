using System;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Item;

namespace DungeonInn.Master
{
    public sealed class ActorArchetypeMaster
    {
        public int Id { get; }
        public string Name { get; }
        public string VisualId { get; }
        public ActorBehaviorType BehaviorType { get; }
        public int SpeciesId { get; }
        public WeaponType DefaultWeaponType { get; }
        public ActorStats BaseStats { get; }
        public int InitialLevel { get; }
        public int LevelTableId { get; }

        public ActorArchetypeMaster(
            int id,
            string name,
            string visualId,
            ActorBehaviorType behaviorType,
            int speciesId,
            WeaponType defaultWeaponType,
            ActorStats baseStats,
            int initialLevel,
            int levelTableId)
        {
            if (id < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Actor archetype name is required.", nameof(name));
            }

            if (string.IsNullOrWhiteSpace(visualId))
            {
                throw new ArgumentException("Actor archetype visual id is required.", nameof(visualId));
            }

            if (initialLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(initialLevel));
            }

            if (levelTableId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(levelTableId));
            }

            if (speciesId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(speciesId));
            }

            Id = id;
            Name = name;
            VisualId = visualId;
            BehaviorType = behaviorType;
            SpeciesId = speciesId;
            DefaultWeaponType = defaultWeaponType == WeaponType.None ? WeaponType.Fist : defaultWeaponType;
            BaseStats = baseStats ?? throw new ArgumentNullException(nameof(baseStats));
            InitialLevel = initialLevel;
            LevelTableId = levelTableId;
        }
    }
}
