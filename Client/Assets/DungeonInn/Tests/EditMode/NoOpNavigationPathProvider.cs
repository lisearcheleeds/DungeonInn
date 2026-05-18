using System.Collections.Generic;
using DungeonInn.Application.Actors.Movement;
using DungeonInn.Domain.Map;

namespace DungeonInn.Tests.EditMode
{
    sealed class NoOpNavigationPathProvider : INavigationPathProvider
    {
        public IReadOnlyList<GridPosition> TryFindPath(
            MapLayerId layerId,
            GridPosition start,
            GridPosition goal)
        {
            return null;
        }
    }
}
