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
            LayerPosition start,
            LayerPosition goal);

        void InvalidatePath(Guid actorId);
        void InvalidateLayerPaths(MapLayerId layerId);
        void RemovePathState(Guid actorId);
    }
}
