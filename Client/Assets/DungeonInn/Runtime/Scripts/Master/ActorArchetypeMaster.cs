using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Domain.Actor;

namespace DungeonInn.Master
{
    public sealed class ActorArchetypeMaster
    {
        public int Id { get; }
        public string Name { get; }
        public ActorBehaviorType BehaviorType { get; }
        public ActorStats BaseStats { get; }
        public int InitialLevel { get; }
        public IReadOnlyList<int> InitialEquipmentItemIds { get; }
        public IReadOnlyList<int> InitialInventoryItemIds { get; }

        public ActorArchetypeMaster(
            int id,
            string name,
            ActorBehaviorType behaviorType,
            ActorStats baseStats,
            int initialLevel,
            IReadOnlyList<int> initialEquipmentItemIds,
            IReadOnlyList<int> initialInventoryItemIds)
        {
            if (id < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Actor archetype name is required.", nameof(name));
            }

            if (initialLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(initialLevel));
            }

            Id = id;
            Name = name;
            BehaviorType = behaviorType;
            BaseStats = baseStats ?? throw new ArgumentNullException(nameof(baseStats));
            InitialLevel = initialLevel;
            InitialEquipmentItemIds = (initialEquipmentItemIds ?? Array.Empty<int>()).ToArray();
            InitialInventoryItemIds = (initialInventoryItemIds ?? Array.Empty<int>()).ToArray();
        }
    }
}
