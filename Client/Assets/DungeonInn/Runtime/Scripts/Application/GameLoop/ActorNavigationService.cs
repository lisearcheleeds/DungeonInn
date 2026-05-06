using System;
using System.Collections.Generic;
using DungeonInn.Application.Pathfinding;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.GameLoop
{
    public sealed class ActorNavigationService : IActorNavigationService
    {
        readonly Dictionary<Guid, ActorPathState> pathStates = new();

        public ActorPathState GetOrComputePathState(
            Guid actorId,
            MapLayer layer,
            Func<GridPosition, bool> isWalkable,
            GridPosition startGrid,
            GridPosition goalGrid)
        {
            if (!pathStates.TryGetValue(actorId, out var state))
            {
                state = new ActorPathState();
                pathStates[actorId] = state;
            }

            if (!state.NeedsRecalculation(goalGrid))
            {
                return state;
            }

            var path = AStarPathfinder.FindPath(layer, isWalkable, startGrid, goalGrid);
            if (path == null)
            {
                state.MarkFailed();
            }
            else
            {
                state.SetPath(path, goalGrid);
            }

            return state;
        }

        public void InvalidatePath(Guid actorId)
        {
            if (pathStates.TryGetValue(actorId, out var state))
            {
                state.Invalidate();
            }
        }
    }
}
