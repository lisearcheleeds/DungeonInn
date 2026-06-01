using System;
using VContainer;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.Actors.Movement
{
    public sealed class MoveActorTowardDestinationUseCase
    {
        readonly ActorMovementService actorMovementService;

        [Inject]
        public MoveActorTowardDestinationUseCase(ActorMovementService actorMovementService)
        {
            this.actorMovementService = actorMovementService
                ?? throw new ArgumentNullException(nameof(actorMovementService));
        }

        public bool Execute(
            Actor actor,
            LayerPosition destination,
            MapLayer layer,
            IGridWalkability walkability,
            float speedMetersPerSecond,
            float deltaGameSeconds)
        {
            return Execute(
                actor,
                destination,
                layer,
                walkability,
                speedMetersPerSecond,
                deltaGameSeconds,
                layer.CellSizeMeters * 0.5f);
        }

        public bool Execute(
            Actor actor,
            LayerPosition destination,
            MapLayer layer,
            IGridWalkability walkability,
            float speedMetersPerSecond,
            float deltaGameSeconds,
            float arrivalRadiusMeters)
        {
            return actorMovementService.MoveToward(
                actor,
                destination,
                layer,
                walkability,
                speedMetersPerSecond,
                deltaGameSeconds,
                arrivalRadiusMeters,
                snapToDestinationOnArrival: false);
        }
    }
}
