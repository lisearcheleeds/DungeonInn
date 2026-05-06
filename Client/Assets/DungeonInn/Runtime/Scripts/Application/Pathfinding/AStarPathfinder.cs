using System;
using System.Collections.Generic;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.Pathfinding
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
            if (start.Equals(goal))
            {
                return Array.Empty<GridPosition>();
            }

            if (!isWalkable(goal))
            {
                return null;
            }

            var openSet = new List<GridPosition> { start };
            var cameFrom = new Dictionary<GridPosition, GridPosition>();
            var gScore = new Dictionary<GridPosition, int> { [start] = 0 };
            var fScore = new Dictionary<GridPosition, int> { [start] = Heuristic(start, goal) };

            while (openSet.Count > 0)
            {
                var current = PopLowestF(openSet, fScore);

                if (current.Equals(goal))
                {
                    return ReconstructPath(cameFrom, current);
                }

                foreach (var (dx, dz) in Directions)
                {
                    var neighbor = new GridPosition(current.X + dx, current.Z + dz);
                    if (!layer.Contains(neighbor) || !isWalkable(neighbor))
                    {
                        continue;
                    }

                    var tentativeG = gScore[current] + 1;
                    if (gScore.TryGetValue(neighbor, out var knownG) && tentativeG >= knownG)
                    {
                        continue;
                    }

                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + Heuristic(neighbor, goal);
                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }

            return null;
        }

        static GridPosition PopLowestF(List<GridPosition> openSet, Dictionary<GridPosition, int> fScore)
        {
            var bestIndex = 0;
            var bestF = fScore.TryGetValue(openSet[0], out var f0) ? f0 : int.MaxValue;
            for (var i = 1; i < openSet.Count; i++)
            {
                var f = fScore.TryGetValue(openSet[i], out var fi) ? fi : int.MaxValue;
                if (f < bestF)
                {
                    bestF = f;
                    bestIndex = i;
                }
            }

            var result = openSet[bestIndex];
            openSet.RemoveAt(bestIndex);
            return result;
        }

        static IReadOnlyList<GridPosition> ReconstructPath(
            Dictionary<GridPosition, GridPosition> cameFrom,
            GridPosition current)
        {
            var path = new List<GridPosition>();
            while (cameFrom.ContainsKey(current))
            {
                path.Add(current);
                current = cameFrom[current];
            }
            path.Reverse();
            return path;
        }

        static int Heuristic(GridPosition a, GridPosition b)
        {
            return Math.Abs(a.X - b.X) + Math.Abs(a.Z - b.Z);
        }
    }
}
