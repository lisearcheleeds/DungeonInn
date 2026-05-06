using System;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Map;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class MoveActorTowardDestinationUseCase
    {
        // TODO: 到着距離・移動速度をマスタから取得する
        const float ArrivalDistanceMeters = 2.5f;

        readonly IActorNavigationService navigationService;

        [Inject]
        public MoveActorTowardDestinationUseCase(IActorNavigationService navigationService)
        {
            this.navigationService = navigationService
                ?? throw new ArgumentNullException(nameof(navigationService));
        }

        public bool Execute(
            Actor actor,
            LayerPosition destination,
            MapLayer layer,
            Func<GridPosition, bool> isWalkable,
            float speedMetersPerSecond,
            float deltaGameSeconds)
        {
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            if (layer == null) throw new ArgumentNullException(nameof(layer));
            if (isWalkable == null) throw new ArgumentNullException(nameof(isWalkable));

            var distSq = actor.Position.DistanceSquaredTo(destination);
            if (distSq <= ArrivalDistanceMeters * ArrivalDistanceMeters)
            {
                actor.MoveTo(destination);
                return true;
            }

            var startGrid = layer.ToGridPosition(actor.Position);
            var goalGrid = layer.ToGridPosition(destination);

            var pathState = navigationService.GetOrComputePathState(
                actor.Id, layer, isWalkable, startGrid, goalGrid);

            if (pathState.HasFailed)
            {
                return false;
            }

            if (!pathState.TryGetCurrentWaypoint(out var nextWaypointGrid))
            {
                actor.MoveTo(destination);
                return true;
            }

            var nextWaypoint = layer.GetCellCenter(nextWaypointGrid);
            var waypointDistSq = actor.Position.DistanceSquaredTo(nextWaypoint);
            var step = speedMetersPerSecond * deltaGameSeconds;

            if (step * step >= waypointDistSq)
            {
                actor.MoveTo(nextWaypoint);
                pathState.AdvanceWaypoint();
            }
            else
            {
                var dist = (float)Math.Sqrt(waypointDistSq);
                var ratio = step / dist;
                actor.MoveTo(new LayerPosition(
                    actor.Position.LayerId,
                    actor.Position.X + (nextWaypoint.X - actor.Position.X) * ratio,
                    actor.Position.Z + (nextWaypoint.Z - actor.Position.Z) * ratio));
            }

            return actor.Position.DistanceSquaredTo(destination) <= ArrivalDistanceMeters * ArrivalDistanceMeters;
        }
    }
}
