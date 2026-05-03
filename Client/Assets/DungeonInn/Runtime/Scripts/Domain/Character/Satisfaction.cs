using System;

namespace DungeonInn.Domain.Character
{
    public readonly struct Satisfaction : IEquatable<Satisfaction>
    {
        public float Value { get; }

        public Satisfaction(float value)
        {
            if (value < 0f)
            {
                Value = 0f;
            }
            else if (value > 1f)
            {
                Value = 1f;
            }
            else
            {
                Value = value;
            }
        }

        public bool IsAboveThreshold(float threshold)
        {
            return Value >= threshold;
        }

        public bool Equals(Satisfaction other)
        {
            return Value.Equals(other.Value);
        }

        public override bool Equals(object obj)
        {
            return obj is Satisfaction other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static bool operator ==(Satisfaction left, Satisfaction right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Satisfaction left, Satisfaction right)
        {
            return !left.Equals(right);
        }
    }
}
