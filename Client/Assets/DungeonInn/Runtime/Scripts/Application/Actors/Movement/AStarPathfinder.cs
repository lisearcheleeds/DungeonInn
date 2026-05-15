using System;
using System.Collections.Generic;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.Actors.Movement
{
    public static class AStarPathfinder
    {
        static readonly (int dx, int dz)[] Directions = { (0, 1), (0, -1), (1, 0), (-1, 0) };

        public static IReadOnlyList<GridPosition> FindPath(
            MapLayer layer,
            Func<GridPosition, bool> isWalkable,
            GridPosition start,
            GridPosition goal)
        {
            var openQueue = new SortedSet<OpenSetNode>(OpenSetNodeComparer.Instance);
            var openSet = new HashSet<GridPosition>();
            var cameFrom = new Dictionary<GridPosition, GridPosition>();
            var gScore = new Dictionary<GridPosition, int>();
            var fScore = new Dictionary<GridPosition, int>();
            var path = new List<GridPosition>();
            return TryFindPath(layer, isWalkable, start, goal, openQueue, openSet, cameFrom, gScore, fScore, path)
                ? path
                : null;
        }

        public static bool TryFindPath(
            MapLayer layer,
            Func<GridPosition, bool> isWalkable,
            GridPosition start,
            GridPosition goal,
            SortedSet<OpenSetNode> openQueue,
            HashSet<GridPosition> openSet,
            Dictionary<GridPosition, GridPosition> cameFrom,
            Dictionary<GridPosition, int> gScore,
            Dictionary<GridPosition, int> fScore,
            List<GridPosition> path)
        {
            openQueue.Clear();
            openSet.Clear();
            cameFrom.Clear();
            gScore.Clear();
            fScore.Clear();
            path.Clear();

            if (start.Equals(goal))
            {
                return true;
            }

            if (!isWalkable(goal))
            {
                return false;
            }

            var sequence = 0;
            openSet.Add(start);
            openQueue.Add(new OpenSetNode(start, Heuristic(start, goal), sequence));
            gScore[start] = 0;
            fScore[start] = Heuristic(start, goal);

            while (openQueue.Count > 0)
            {
                var current = PopLowestF(openQueue, openSet);
                if (!current.HasValue)
                {
                    return false;
                }

                if (current.Value.Equals(goal))
                {
                    ReconstructPath(cameFrom, current.Value, path);
                    return true;
                }

                for (var i = 0; i < Directions.Length; i++)
                {
                    var (dx, dz) = Directions[i];
                    var neighbor = new GridPosition(current.Value.X + dx, current.Value.Z + dz);
                    if (!layer.Contains(neighbor) || !isWalkable(neighbor))
                    {
                        continue;
                    }

                    var tentativeG = gScore[current.Value] + 1;
                    if (gScore.TryGetValue(neighbor, out var knownG) && tentativeG >= knownG)
                    {
                        continue;
                    }

                    cameFrom[neighbor] = current.Value;
                    gScore[neighbor] = tentativeG;
                    var estimatedTotalCost = tentativeG + Heuristic(neighbor, goal);
                    fScore[neighbor] = estimatedTotalCost;
                    openSet.Add(neighbor);
                    sequence++;
                    openQueue.Add(new OpenSetNode(neighbor, estimatedTotalCost, sequence));
                }
            }

            return false;
        }

        static GridPosition? PopLowestF(SortedSet<OpenSetNode> openQueue, HashSet<GridPosition> openSet)
        {
            while (openQueue.Count > 0)
            {
                var node = openQueue.Min;
                openQueue.Remove(node);
                if (!openSet.Remove(node.Position))
                {
                    continue;
                }

                return node.Position;
            }

            return null;
        }

        static void ReconstructPath(
            Dictionary<GridPosition, GridPosition> cameFrom,
            GridPosition current,
            List<GridPosition> path)
        {
            while (cameFrom.ContainsKey(current))
            {
                path.Add(current);
                current = cameFrom[current];
            }

            path.Reverse();
        }

        static int Heuristic(GridPosition first, GridPosition second)
        {
            return Math.Abs(first.X - second.X) + Math.Abs(first.Z - second.Z);
        }

        public readonly struct OpenSetNode
        {
            public GridPosition Position { get; }
            public int EstimatedTotalCost { get; }
            public int Sequence { get; }

            public OpenSetNode(GridPosition position, int estimatedTotalCost, int sequence)
            {
                Position = position;
                EstimatedTotalCost = estimatedTotalCost;
                Sequence = sequence;
            }
        }

        public sealed class OpenSetNodeComparer : IComparer<OpenSetNode>
        {
            public static readonly OpenSetNodeComparer Instance = new();

            public int Compare(OpenSetNode x, OpenSetNode y)
            {
                var estimatedTotalCostComparison = x.EstimatedTotalCost.CompareTo(y.EstimatedTotalCost);
                if (estimatedTotalCostComparison != 0)
                {
                    return estimatedTotalCostComparison;
                }

                var xComparison = x.Position.X.CompareTo(y.Position.X);
                if (xComparison != 0)
                {
                    return xComparison;
                }

                var zComparison = x.Position.Z.CompareTo(y.Position.Z);
                if (zComparison != 0)
                {
                    return zComparison;
                }

                return x.Sequence.CompareTo(y.Sequence);
            }
        }
    }
}
