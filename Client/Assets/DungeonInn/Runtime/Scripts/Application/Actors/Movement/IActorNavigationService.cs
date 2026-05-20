using System;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.Actors.Movement
{
    public interface IActorNavigationService
    {
        ActorPathState GetOrComputePathState(
            Guid actorId,
            MapLayer layer,
            IGridWalkability walkability,
            GridPosition startGrid,
            GridPosition goalGrid);

        void InvalidatePath(Guid actorId);
        void RemovePathState(Guid actorId);
    }
}
