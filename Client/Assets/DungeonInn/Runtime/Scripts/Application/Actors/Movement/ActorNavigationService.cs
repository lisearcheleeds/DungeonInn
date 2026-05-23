using System;
using System.Collections.Generic;
using R3;
using VContainer;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.Actors.Movement
{
    public sealed class ActorNavigationService : IActorNavigationService, IDisposable
    {
        readonly Dictionary<Guid, ActorPathState> pathStates = new();
        readonly SortedSet<AStarPathfinder.OpenSetNode> openQueue =
            new(AStarPathfinder.OpenSetNodeComparer.Instance);
        readonly HashSet<GridPosition> openSet = new();
        readonly Dictionary<GridPosition, GridPosition> cameFrom = new();
        readonly Dictionary<GridPosition, int> gScore = new();
        readonly Dictionary<GridPosition, int> fScore = new();
        readonly List<GridPosition> pathBuffer = new();
        readonly INavigationPathProvider navigationPathProvider;
        DisposableBag bag;

        [Inject]
        public ActorNavigationService(
            IEventSubscriber eventSubscriber,
            INavigationPathProvider navigationPathProvider)
        {
            if (eventSubscriber == null)
            {
                throw new ArgumentNullException(nameof(eventSubscriber));
            }

            this.navigationPathProvider = navigationPathProvider ??
                throw new ArgumentNullException(nameof(navigationPathProvider));

            eventSubscriber.OnEvent<ActorDeparted>()
                .Subscribe(gameEvent => RemovePathState(gameEvent.ActorId))
                .AddTo(ref bag);
            eventSubscriber.OnEvent<ActorDefeated>()
                .Subscribe(gameEvent => RemovePathState(gameEvent.ActorId))
                .AddTo(ref bag);
        }

        public ActorPathState GetOrComputePathState(
            Guid actorId,
            MapLayer layer,
            IGridWalkability walkability,
            GridPosition startGrid,
            GridPosition goalGrid)
        {
            if (!pathStates.TryGetValue(actorId, out var state))
            {
                state = new ActorPathState();
                pathStates[actorId] = state;
            }

            if (!state.TryConsumeRecalculationRequest(layer.Id, startGrid, goalGrid))
            {
                return state;
            }

            var navPath = navigationPathProvider.TryFindPath(layer.Id, startGrid, goalGrid);
            if (navPath != null && IsGridPathUsable(layer, walkability, startGrid, navPath))
            {
                state.SetPath(layer.Id, startGrid, navPath, goalGrid);
                return state;
            }

            if (!AStarPathfinder.TryFindPath(
                layer,
                walkability,
                startGrid,
                goalGrid,
                openQueue,
                openSet,
                cameFrom,
                gScore,
                fScore,
                pathBuffer))
            {
                state.MarkFailed(layer.Id, startGrid, goalGrid);
            }
            else
            {
                state.SetPath(layer.Id, startGrid, pathBuffer, goalGrid);
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

        public void RemovePathState(Guid actorId)
        {
            pathStates.Remove(actorId);
        }

        static bool IsGridPathUsable(
            MapLayer layer,
            IGridWalkability walkability,
            GridPosition startGrid,
            IReadOnlyList<GridPosition> path)
        {
            if (path.Count == 0)
            {
                return false;
            }

            var previous = startGrid;
            for (var i = 0; i < path.Count; i++)
            {
                if (!IsClearCardinalSegment(layer, walkability, previous, path[i]))
                {
                    return false;
                }

                previous = path[i];
            }

            return true;
        }

        static bool IsClearCardinalSegment(
            MapLayer layer,
            IGridWalkability walkability,
            GridPosition first,
            GridPosition second)
        {
            if (first.Equals(second) || (first.X != second.X && first.Z != second.Z))
            {
                return false;
            }

            var stepX = Math.Sign(second.X - first.X);
            var stepZ = Math.Sign(second.Z - first.Z);
            var current = new GridPosition(first.X + stepX, first.Z + stepZ);
            while (true)
            {
                if (!layer.Contains(current) || !walkability.IsWalkable(current))
                {
                    return false;
                }

                if (current.Equals(second))
                {
                    return true;
                }

                current = new GridPosition(current.X + stepX, current.Z + stepZ);
            }
        }

        public void Dispose()
        {
            bag.Dispose();
        }
    }
}
