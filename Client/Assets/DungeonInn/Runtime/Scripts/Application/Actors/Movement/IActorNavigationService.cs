using System;
using DungeonInn.Application.Actors.Movement;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.Actors.Movement
{
    public interface IActorNavigationService
    {
        ActorPathState GetOrComputePathState(
            Guid actorId,
            MapLayer layer,
            System.Func<GridPosition, bool> isWalkable,
            GridPosition startGrid,
            GridPosition goalGrid);

        void InvalidatePath(Guid actorId);
        void RemovePathState(Guid actorId);
    }
}
