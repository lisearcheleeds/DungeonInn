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
            var openSet = new List<GridPosition>();
            var cameFrom = new Dictionary<GridPosition, GridPosition>();
            var gScore = new Dictionary<GridPosition, int>();
            var fScore = new Dictionary<GridPosition, int>();
            var path = new List<GridPosition>();
            return TryFindPath(layer, isWalkable, start, goal, openSet, cameFrom, gScore, fScore, path)
                ? path
                : null;
        }

        public static bool TryFindPath(
            MapLayer layer,
            Func<GridPosition, bool> isWalkable,
            GridPosition start,
            GridPosition goal,
            List<GridPosition> openSet,
            Dictionary<GridPosition, GridPosition> cameFrom,
            Dictionary<GridPosition, int> gScore,
            Dictionary<GridPosition, int> fScore,
            List<GridPosition> path)
        {
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

            openSet.Add(start);
            gScore[start] = 0;
            fScore[start] = Heuristic(start, goal);

            while (openSet.Count > 0)
            {
                var current = PopLowestF(openSet, fScore);

                if (current.Equals(goal))
                {
                    ReconstructPath(cameFrom, current, path);
                    return true;
                }

                for (var i = 0; i < Directions.Length; i++)
                {
                    var (dx, dz) = Directions[i];
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

            return false;
        }

        static GridPosition PopLowestF(List<GridPosition> openSet, Dictionary<GridPosition, int> fScore)
        {
            var bestIndex = 0;
            var bestF = fScore.TryGetValue(openSet[0], out var initialScore) ? initialScore : int.MaxValue;
            for (var i = 1; i < openSet.Count; i++)
            {
                var candidateScore = fScore.TryGetValue(openSet[i], out var score) ? score : int.MaxValue;
                if (candidateScore < bestF)
                {
                    bestF = candidateScore;
                    bestIndex = i;
                }
            }

            var result = openSet[bestIndex];
            openSet.RemoveAt(bestIndex);
            return result;
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
    }
}
