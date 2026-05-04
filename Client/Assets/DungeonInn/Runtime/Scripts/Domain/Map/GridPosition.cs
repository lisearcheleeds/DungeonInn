using System;

namespace DungeonInn.Domain.Map
{
    public readonly struct GridPosition : IEquatable<GridPosition>
    {
        public int X { get; }
        public int Z { get; }

        public GridPosition(int x, int z)
        {
            X = x;
            Z = z;
        }

        public bool Equals(GridPosition other)
        {
            return X == other.X && Z == other.Z;
        }

        public override bool Equals(object obj)
        {
            return obj is GridPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Z);
        }
    }
}
