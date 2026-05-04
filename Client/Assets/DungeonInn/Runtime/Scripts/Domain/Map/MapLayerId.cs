using System;

namespace DungeonInn.Domain.Map
{
    public readonly struct MapLayerId : IEquatable<MapLayerId>
    {
        public int Value { get; }

        public MapLayerId(int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            Value = value;
        }

        public static MapLayerId Ground => new(0);

        public static MapLayerId DungeonFloor(int floorIndex)
        {
            if (floorIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(floorIndex));
            }

            return new MapLayerId(floorIndex);
        }

        public bool Equals(MapLayerId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is MapLayerId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }
    }
}
