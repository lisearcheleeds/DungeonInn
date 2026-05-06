using System.Collections.Generic;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.Pathfinding
{
    public sealed class ActorPathState
    {
        IReadOnlyList<GridPosition> path;

        public GridPosition CachedGoal { get; private set; }
        public bool IsDirty { get; private set; } = true;
        public bool HasFailed { get; private set; }

        int waypointIndex;

        public bool NeedsRecalculation(GridPosition goalGrid)
        {
            return IsDirty || !CachedGoal.Equals(goalGrid);
        }

        public bool TryGetCurrentWaypoint(out GridPosition waypoint)
        {
            if (path == null || waypointIndex >= path.Count)
            {
                waypoint = default;
                return false;
            }

            waypoint = path[waypointIndex];
            return true;
        }

        public void AdvanceWaypoint()
        {
            waypointIndex++;
        }

        public void SetPath(IReadOnlyList<GridPosition> newPath, GridPosition goal)
        {
            path = newPath;
            CachedGoal = goal;
            waypointIndex = 0;
            IsDirty = false;
            HasFailed = false;
        }

        public void MarkFailed()
        {
            path = null;
            IsDirty = false;
            HasFailed = true;
        }

        public void Invalidate()
        {
            IsDirty = true;
            HasFailed = false;
        }
    }
}
