using System.Collections.Generic;
using DungeonInn.Application.Actors.Movement;
using DungeonInn.Domain.Map;

namespace DungeonInn.Tests.EditMode
{
    sealed class NoOpNavigationPathProvider : INavigationPathProvider
    {
        public IReadOnlyList<LayerPosition> TryFindPath(
            MapLayer layer,
            LayerPosition start,
            LayerPosition goal)
        {
            return null;
        }
    }
}

