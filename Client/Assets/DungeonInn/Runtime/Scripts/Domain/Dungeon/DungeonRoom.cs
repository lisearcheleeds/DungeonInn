using System;
using System.Collections.Generic;
using DungeonInn.Domain.Map;

namespace DungeonInn.Domain.Dungeon
{
    public sealed class DungeonRoom
    {
        public int Id { get; }
        public GridPosition Center { get; }
        public int Width { get; }
        public int Depth { get; }
        public int RouteDepth { get; }
        public IReadOnlyList<GridPosition> Cells { get; }

        public DungeonRoom(
            int id,
            GridPosition center,
            int width,
            int depth,
            int routeDepth,
            IReadOnlyList<GridPosition> cells)
        {
            if (id < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            if (width < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (depth < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(depth));
            }

            if (routeDepth < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(routeDepth));
            }

            Id = id;
            Center = center;
            Width = width;
            Depth = depth;
            RouteDepth = routeDepth;
            Cells = cells ?? throw new ArgumentNullException(nameof(cells));
        }
    }
}
