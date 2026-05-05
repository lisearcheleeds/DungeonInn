using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonInn.Domain.Combat
{
    public sealed class CombatEffectGraphValidator
    {
        public void Validate(WeaponAttackSpec attackSpec)
        {
            if (attackSpec == null)
            {
                throw new ArgumentNullException(nameof(attackSpec));
            }

            var nodesById = BuildNodeMap(attackSpec);
            ValidateRootNodes(attackSpec, nodesById);
            ValidateLinks(attackSpec, nodesById);
            ValidateOnTickLinks(nodesById.Values);
            ValidateNoCycles(attackSpec, nodesById);
        }

        static Dictionary<int, CombatEffectNodeSpec> BuildNodeMap(WeaponAttackSpec attackSpec)
        {
            var nodesById = new Dictionary<int, CombatEffectNodeSpec>();
            foreach (var node in attackSpec.Nodes)
            {
                if (nodesById.ContainsKey(node.Id))
                {
                    throw new InvalidOperationException("Combat effect node id must be unique.");
                }

                nodesById.Add(node.Id, node);
            }

            return nodesById;
        }

        static void ValidateRootNodes(WeaponAttackSpec attackSpec, IReadOnlyDictionary<int, CombatEffectNodeSpec> nodesById)
        {
            foreach (var rootNodeId in attackSpec.RootNodeIds)
            {
                if (!nodesById.ContainsKey(rootNodeId))
                {
                    throw new InvalidOperationException("Combat effect root node does not exist.");
                }
            }
        }

        static void ValidateLinks(WeaponAttackSpec attackSpec, IReadOnlyDictionary<int, CombatEffectNodeSpec> nodesById)
        {
            foreach (var node in attackSpec.Nodes)
            {
                foreach (var link in node.Links)
                {
                    if (!nodesById.ContainsKey(link.TargetNodeId))
                    {
                        throw new InvalidOperationException("Combat effect link target node does not exist.");
                    }
                }
            }
        }

        static void ValidateOnTickLinks(IEnumerable<CombatEffectNodeSpec> nodes)
        {
            foreach (var node in nodes)
            {
                var hasOnTick = node.Links.Any(x => x.TriggerType == CombatEffectTriggerType.OnTick);
                if (!hasOnTick)
                {
                    continue;
                }

                if (node.Type != CombatEffectNodeType.Area ||
                    node.AreaSpec == null ||
                    node.AreaSpec.DurationType != AttackAreaDurationType.Duration)
                {
                    throw new InvalidOperationException("OnTick links are only allowed on duration area nodes.");
                }
            }
        }

        static void ValidateNoCycles(WeaponAttackSpec attackSpec, IReadOnlyDictionary<int, CombatEffectNodeSpec> nodesById)
        {
            var visited = new HashSet<int>();
            var visiting = new HashSet<int>();

            foreach (var node in attackSpec.Nodes)
            {
                Visit(node.Id, nodesById, visited, visiting);
            }
        }

        static void Visit(
            int nodeId,
            IReadOnlyDictionary<int, CombatEffectNodeSpec> nodesById,
            ISet<int> visited,
            ISet<int> visiting)
        {
            if (visited.Contains(nodeId))
            {
                return;
            }

            if (visiting.Contains(nodeId))
            {
                throw new InvalidOperationException("Combat effect graph cannot contain cycles.");
            }

            visiting.Add(nodeId);
            var node = nodesById[nodeId];
            foreach (var link in node.Links)
            {
                Visit(link.TargetNodeId, nodesById, visited, visiting);
            }

            visiting.Remove(nodeId);
            visited.Add(nodeId);
        }
    }
}
