using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonInn.Domain.Combat
{
    public sealed class WeaponAttackSpec
    {
        public int Id { get; }
        public IReadOnlyList<int> RootNodeIds { get; }
        public IReadOnlyList<CombatEffectNodeSpec> Nodes { get; }
        public int MaxGenerationDepth { get; }

        public WeaponAttackSpec(
            int id,
            IReadOnlyList<int> rootNodeIds,
            IReadOnlyList<CombatEffectNodeSpec> nodes,
            int maxGenerationDepth)
        {
            if (id < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            if (rootNodeIds == null || rootNodeIds.Count == 0)
            {
                throw new ArgumentException("Weapon attack spec requires root nodes.", nameof(rootNodeIds));
            }

            if (nodes == null || nodes.Count == 0)
            {
                throw new ArgumentException("Weapon attack spec requires nodes.", nameof(nodes));
            }

            if (maxGenerationDepth < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxGenerationDepth));
            }

            Id = id;
            RootNodeIds = rootNodeIds.ToArray();
            Nodes = nodes.ToArray();
            MaxGenerationDepth = maxGenerationDepth;
        }
    }
}
