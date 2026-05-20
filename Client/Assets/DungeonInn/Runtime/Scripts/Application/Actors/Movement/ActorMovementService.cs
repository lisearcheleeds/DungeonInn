using System;
using VContainer;
using DungeonInn.Application.Combat;
using DungeonInn.Application.World;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.Actors.Movement
{
    public sealed class ActorMovementService
    {
        readonly IActorNavigationService navigationService;
        readonly ActorSpatialIndexService actorSpatialIndexService;
        readonly ActorViewDataStore actorViewDataStore;

        [Inject]
        public ActorMovementService(
            IActorNavigationService navigationService,
            ActorSpatialIndexService actorSpatialIndexService,
            ActorViewDataStore actorViewDataStore)
        {
            this.navigationService = navigationService
                ?? throw new ArgumentNullException(nameof(navigationService));
            this.actorSpatialIndexService = actorSpatialIndexService
                ?? throw new ArgumentNullException(nameof(actorSpatialIndexService));
            this.actorViewDataStore = actorViewDataStore
                ?? throw new ArgumentNullException(nameof(actorViewDataStore));
        }

        public bool MoveToward(
            Actor actor,
            LayerPosition destination,
            MapLayer layer,
            IGridWalkability walkability,
            float speedMetersPerSecond,
            float deltaGameSeconds,
            float arrivalDistanceMeters,
            bool snapToDestinationOnArrival)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            if (layer == null)
            {
                throw new ArgumentNullException(nameof(layer));
            }

            if (walkability == null)
            {
                throw new ArgumentNullException(nameof(walkability));
            }

            var arrivalDistance = Math.Max(0f, arrivalDistanceMeters);
            var distSq = actor.Position.DistanceSquaredTo(destination);
            if (distSq <= arrivalDistance * arrivalDistance)
            {
                if (snapToDestinationOnArrival)
                {
                    MoveTo(actor, destination);
                }

                return true;
            }

            var startGrid = layer.ToGridPosition(actor.Position);
            var goalGrid = layer.ToGridPosition(destination);
            var pathState = navigationService.GetOrComputePathState(
                actor.Id,
                layer,
                walkability,
                startGrid,
                goalGrid);

            if (pathState.HasFailed)
            {
                return false;
            }

            if (!pathState.TryGetCurrentWaypoint(out var nextWaypointGrid))
            {
                if (snapToDestinationOnArrival)
                {
                    MoveTo(actor, destination);
                }

                return true;
            }

            var nextWaypoint = layer.GetCellCenter(nextWaypointGrid);
            var waypointDistSq = actor.Position.DistanceSquaredTo(nextWaypoint);
            var step = speedMetersPerSecond * deltaGameSeconds;

            if (waypointDistSq <= step * step)
            {
                MoveTo(actor, nextWaypoint);
                pathState.AdvanceWaypoint();
            }
            else
            {
                var dist = (float)Math.Sqrt(waypointDistSq);
                var ratio = step / dist;
                MoveTo(actor, new LayerPosition(
                    actor.Position.LayerId,
                    actor.Position.X + (nextWaypoint.X - actor.Position.X) * ratio,
                    actor.Position.Z + (nextWaypoint.Z - actor.Position.Z) * ratio));
            }

            return actor.Position.DistanceSquaredTo(destination) <= arrivalDistance * arrivalDistance;
        }

        void MoveTo(Actor actor, LayerPosition position)
        {
            actor.MoveTo(position);
            actorSpatialIndexService.SyncActor(actor);
            actorViewDataStore.SyncActor(actor);
        }
    }
}
