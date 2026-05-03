using System;

namespace DungeonInn.Domain.Inn
{
    public readonly struct Money : IEquatable<Money>, IComparable<Money>
    {
        public int Value { get; }

        public Money(int value)
        {
            Value = Math.Max(0, value);
        }

        public bool Equals(Money other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is Money other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public int CompareTo(Money other)
        {
            return Value.CompareTo(other.Value);
        }

        public static Money operator +(Money a, Money b)
        {
            return new Money(a.Value + b.Value);
        }

        public static Money operator -(Money a, Money b)
        {
            return new Money(a.Value - b.Value);
        }

        public static bool operator >=(Money a, Money b)
        {
            return a.Value >= b.Value;
        }

        public static bool operator <=(Money a, Money b)
        {
            return a.Value <= b.Value;
        }

        public static bool operator ==(Money left, Money right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Money left, Money right)
        {
            return !left.Equals(right);
        }
    }
}
