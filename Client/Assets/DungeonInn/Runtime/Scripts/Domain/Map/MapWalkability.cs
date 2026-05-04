using System;

namespace DungeonInn.Domain.Map
{
    public static class MapWalkability
    {
        public static bool IsWalkable(
            MapLayer layer,
            Func<GridPosition, bool> isCellWalkable,
            LayerPosition position,
            float agentRadius)
        {
            if (!layer.Contains(position))
            {
                return false;
            }

            var radius = Math.Max(0f, agentRadius);
            return IsPointWalkable(layer, isCellWalkable, position.X, position.Z)
                && IsPointWalkable(layer, isCellWalkable, position.X + radius, position.Z)
                && IsPointWalkable(layer, isCellWalkable, position.X - radius, position.Z)
                && IsPointWalkable(layer, isCellWalkable, position.X, position.Z + radius)
                && IsPointWalkable(layer, isCellWalkable, position.X, position.Z - radius)
                && IsPointWalkable(layer, isCellWalkable, position.X + radius, position.Z + radius)
                && IsPointWalkable(layer, isCellWalkable, position.X + radius, position.Z - radius)
                && IsPointWalkable(layer, isCellWalkable, position.X - radius, position.Z + radius)
                && IsPointWalkable(layer, isCellWalkable, position.X - radius, position.Z - radius);
        }

        static bool IsPointWalkable(
            MapLayer layer,
            Func<GridPosition, bool> isCellWalkable,
            float x,
            float z)
        {
            var position = new LayerPosition(layer.Id, x, z);
            if (!layer.Contains(position))
            {
                return false;
            }

            return isCellWalkable(layer.ToGridPosition(position));
        }
    }
}
