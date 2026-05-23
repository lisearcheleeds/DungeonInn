using System.Collections.Generic;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.Actors.Movement
{
    public interface INavigationPathProvider
    {
        /// <summary>
        /// Returned paths may be backed by a provider-owned buffer. Callers must consume the result in the
        /// same frame and must not retain it after another path request.
        /// </summary>
        IReadOnlyList<GridPosition> TryFindPath(MapLayerId layerId, GridPosition start, GridPosition goal);
    }
}
