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
        readonly IWorldGameSettingsRepository worldGameSettingsRepository;

        [Inject]
        public MoveActorTowardDestinationUseCase(
            ActorMovementService actorMovementService,
            IWorldGameSettingsRepository worldGameSettingsRepository)
        {
            this.actorMovementService = actorMovementService
                ?? throw new ArgumentNullException(nameof(actorMovementService));
            this.worldGameSettingsRepository = worldGameSettingsRepository
                ?? throw new ArgumentNullException(nameof(worldGameSettingsRepository));
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
                worldGameSettingsRepository.GetActorSimulationSettings().MoveArrivalDistanceMeters,
                snapToDestinationOnArrival: true);
        }
    }
}
