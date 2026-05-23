using System;
using VContainer;
using DungeonInn.Application.World;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.Actors.Movement
{
    public sealed class MoveActorTowardDestinationUseCase
    {
        readonly ActorMovementService actorMovementService;
        readonly ActorSimulationSettings actorSimulationSettings;

        [Inject]
        public MoveActorTowardDestinationUseCase(
            ActorMovementService actorMovementService,
            ActorSimulationSettings actorSimulationSettings)
        {
            this.actorMovementService = actorMovementService
                ?? throw new ArgumentNullException(nameof(actorMovementService));
            this.actorSimulationSettings = actorSimulationSettings
                ?? throw new ArgumentNullException(nameof(actorSimulationSettings));
        }

        public bool Execute(
            Actor actor,
            LayerPosition destination,
            MapLayer layer,
            IGridWalkability walkability,
            float speedMetersPerSecond,
            float deltaGameSeconds)
        {
            return actorMovementService.MoveToward(
                actor,
                destination,
                layer,
                walkability,
                speedMetersPerSecond,
                deltaGameSeconds,
                actorSimulationSettings.MoveArrivalDistanceMeters,
                snapToDestinationOnArrival: true);
        }
    }
}
