using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonInn.Domain.Combat
{
    public sealed class CombatEffectGraphValidator
    {
        public void Validate(WeaponAttackDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            var nodesById = BuildNodeMap(definition);
            ValidateRootNodes(definition, nodesById);
            ValidateLinks(definition, nodesById);
            ValidateOnTickLinks(nodesById.Values);
            ValidateNoCycles(definition, nodesById);
        }

        static Dictionary<int, CombatEffectNode> BuildNodeMap(WeaponAttackDefinition definition)
        {
            var nodesById = new Dictionary<int, CombatEffectNode>();
            foreach (var node in definition.Nodes)
            {
                if (nodesById.ContainsKey(node.Id))
                {
                    throw new InvalidOperationException("Combat effect node id must be unique.");
                }

                nodesById.Add(node.Id, node);
            }

            return nodesById;
        }

        static void ValidateRootNodes(WeaponAttackDefinition definition, IReadOnlyDictionary<int, CombatEffectNode> nodesById)
        {
            foreach (var rootNodeId in definition.RootNodeIds)
            {
                if (!nodesById.ContainsKey(rootNodeId))
                {
                    throw new InvalidOperationException("Combat effect root node does not exist.");
                }
            }
        }

        static void ValidateLinks(WeaponAttackDefinition definition, IReadOnlyDictionary<int, CombatEffectNode> nodesById)
        {
            foreach (var node in definition.Nodes)
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

        static void ValidateOnTickLinks(IEnumerable<CombatEffectNode> nodes)
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

        static void ValidateNoCycles(WeaponAttackDefinition definition, IReadOnlyDictionary<int, CombatEffectNode> nodesById)
        {
            var visited = new HashSet<int>();
            var visiting = new HashSet<int>();

            foreach (var node in definition.Nodes)
            {
                Visit(node.Id, nodesById, visited, visiting);
            }
        }

        static void Visit(
            int nodeId,
            IReadOnlyDictionary<int, CombatEffectNode> nodesById,
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
