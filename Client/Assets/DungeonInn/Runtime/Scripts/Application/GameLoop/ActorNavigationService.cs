using System;
using System.Collections.Generic;
using R3;
using VContainer;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.Pathfinding;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.GameLoop
{
    public sealed class ActorNavigationService : IActorNavigationService, IDisposable
    {
        readonly Dictionary<Guid, ActorPathState> pathStates = new();
        readonly List<GridPosition> openSet = new();
        readonly Dictionary<GridPosition, GridPosition> cameFrom = new();
        readonly Dictionary<GridPosition, int> gScore = new();
        readonly Dictionary<GridPosition, int> fScore = new();
        readonly List<GridPosition> pathBuffer = new();
        DisposableBag bag;

        [Inject]
        public ActorNavigationService(IEventSubscriber eventSubscriber)
        {
            if (eventSubscriber == null)
            {
                throw new ArgumentNullException(nameof(eventSubscriber));
            }

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

            if (!AStarPathfinder.TryFindPath(
                layer,
                isWalkable,
                startGrid,
                goalGrid,
                openSet,
                cameFrom,
                gScore,
                fScore,
                pathBuffer))
            {
                state.MarkFailed();
            }
            else
            {
                state.SetPath(pathBuffer, goalGrid);
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

        public void Dispose()
        {
            bag.Dispose();
        }
    }
}
