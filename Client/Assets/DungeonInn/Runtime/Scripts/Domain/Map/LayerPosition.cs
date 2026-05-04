using System;

namespace DungeonInn.Domain.Map
{
    public readonly struct LayerPosition
    {
        public MapLayerId LayerId { get; }
        public float X { get; }
        public float Z { get; }

        public LayerPosition(MapLayerId layerId, float x, float z)
        {
            LayerId = layerId;
            X = x;
            Z = z;
        }

        public float DistanceSquaredTo(LayerPosition other)
        {
            if (!LayerId.Equals(other.LayerId))
            {
                throw new InvalidOperationException("Cannot compare positions from different layers.");
            }

            var x = X - other.X;
            var z = Z - other.Z;
            return x * x + z * z;
        }
    }
}
