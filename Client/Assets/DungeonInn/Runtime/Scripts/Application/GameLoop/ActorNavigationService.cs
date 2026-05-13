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
        DisposableBag bag;

        public ActorNavigationService()
        {
        }

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
