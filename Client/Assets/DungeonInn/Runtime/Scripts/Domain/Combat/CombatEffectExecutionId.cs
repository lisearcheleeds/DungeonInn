using System;

namespace DungeonInn.Domain.Combat
{
    public readonly struct CombatEffectExecutionId : IEquatable<CombatEffectExecutionId>
    {
        public Guid Value { get; }

        public CombatEffectExecutionId(Guid value)
        {
            if (value.Equals(Guid.Empty))
            {
                throw new ArgumentException("Combat effect execution id is required.", nameof(value));
            }

            Value = value;
        }

        public static CombatEffectExecutionId New()
        {
            return new CombatEffectExecutionId(Guid.NewGuid());
        }

        public bool Equals(CombatEffectExecutionId other)
        {
            return Value.Equals(other.Value);
        }

        public override bool Equals(object obj)
        {
            return obj is CombatEffectExecutionId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
