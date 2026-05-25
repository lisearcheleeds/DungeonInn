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
        readonly List<LayerPosition> waypointBuffer = new();
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
            LayerPosition start,
            LayerPosition goal)
        {
            var startGrid = layer.ToGridPosition(start);
            var goalGrid = layer.ToGridPosition(goal);
            if (!pathStates.TryGetValue(actorId, out var state))
            {
                state = new ActorPathState();
                pathStates[actorId] = state;
            }

            if (!state.TryConsumeRecalculationRequest(layer.Id, startGrid, goalGrid))
            {
                return state;
            }

            var navPath = navigationPathProvider.TryFindPath(layer, start, goal);
            if (navPath != null)
            {
                state.SetPath(layer, startGrid, navPath, goalGrid);
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
                ConvertGridPathToWaypoints(layer, pathBuffer, waypointBuffer);
                state.SetPath(layer, startGrid, waypointBuffer, goalGrid);
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

        public void InvalidateLayerPaths(MapLayerId layerId)
        {
            foreach (var state in pathStates.Values)
            {
                if (state.CachedLayerId.Equals(layerId))
                {
                    state.Invalidate();
                }
            }
        }

        public void RemovePathState(Guid actorId)
        {
            pathStates.Remove(actorId);
        }

        static void ConvertGridPathToWaypoints(
            MapLayer layer,
            IReadOnlyList<GridPosition> gridPath,
            List<LayerPosition> results)
        {
            results.Clear();
            for (var i = 0; i < gridPath.Count; i++)
            {
                results.Add(layer.GetCellCenter(gridPath[i]));
            }
        }

        public void Dispose()
        {
            bag.Dispose();
        }
    }
}
