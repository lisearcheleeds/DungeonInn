using System;
using DungeonInn.Application.Pathfinding;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.GameLoop
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
