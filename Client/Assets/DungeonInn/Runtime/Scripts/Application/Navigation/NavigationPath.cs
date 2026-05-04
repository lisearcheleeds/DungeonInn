using System;
using System.Collections.Generic;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.Navigation
{
    public sealed class NavigationPath
    {
        public IReadOnlyList<LayerPosition> Points { get; }

        public NavigationPath(IReadOnlyList<LayerPosition> points)
        {
            Points = points ?? throw new ArgumentNullException(nameof(points));
        }
    }
}
