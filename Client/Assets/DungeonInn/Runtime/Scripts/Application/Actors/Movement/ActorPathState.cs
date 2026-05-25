using System.Collections.Generic;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.Actors.Movement
{
    public sealed class ActorPathState
    {
        const int FailedPathRecheckIntervalRequests = 30;

        readonly List<LayerPosition> path = new();
        readonly List<GridPosition> waypointGrids = new();

        public GridPosition CachedGoal { get; private set; }
        public GridPosition CachedStart { get; private set; }
        public MapLayerId CachedLayerId { get; private set; }
        public bool IsDirty { get; private set; } = true;
        public bool HasFailed { get; private set; }

        int waypointIndex;
        int failedPathRecheckCountdown;

        public bool TryConsumeRecalculationRequest(
            MapLayerId layerId,
            GridPosition startGrid,
            GridPosition goalGrid)
        {
            if (IsDirty || !CachedLayerId.Equals(layerId) || !CachedGoal.Equals(goalGrid))
            {
                return true;
            }

            if (HasFailed)
            {
                if (!CachedStart.Equals(startGrid))
                {
                    return true;
                }

                if (failedPathRecheckCountdown <= 0)
                {
                    return true;
                }

                failedPathRecheckCountdown--;
                return false;
            }

            if (path.Count == 0)
            {
                return false;
            }

            if (waypointIndex <= 0)
            {
                return !CachedStart.Equals(startGrid) && !waypointGrids[0].Equals(startGrid);
            }

            var previousWaypoint = waypointGrids[waypointIndex - 1];
            if (previousWaypoint.Equals(startGrid))
            {
                return false;
            }

            if (waypointIndex < waypointGrids.Count && waypointGrids[waypointIndex].Equals(startGrid))
            {
                return false;
            }

            return true;
        }

        public bool TryGetCurrentWaypoint(out LayerPosition waypoint)
        {
            if (path.Count <= waypointIndex)
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

        public void SetPath(
            MapLayer layer,
            GridPosition start,
            IReadOnlyList<LayerPosition> newPath,
            GridPosition goal)
        {
            path.Clear();
            waypointGrids.Clear();
            for (var i = 0; i < newPath.Count; i++)
            {
                path.Add(newPath[i]);
                waypointGrids.Add(layer.ToGridPosition(newPath[i]));
            }

            CachedLayerId = layer.Id;
            CachedStart = start;
            CachedGoal = goal;
            waypointIndex = 0;
            failedPathRecheckCountdown = 0;
            IsDirty = false;
            HasFailed = false;
        }

        public void MarkFailed(MapLayerId layerId, GridPosition start, GridPosition goal)
        {
            path.Clear();
            waypointGrids.Clear();
            CachedLayerId = layerId;
            CachedStart = start;
            CachedGoal = goal;
            failedPathRecheckCountdown = FailedPathRecheckIntervalRequests;
            IsDirty = false;
            HasFailed = true;
        }

        public void Invalidate()
        {
            IsDirty = true;
            HasFailed = false;
            failedPathRecheckCountdown = 0;
        }
    }
}
