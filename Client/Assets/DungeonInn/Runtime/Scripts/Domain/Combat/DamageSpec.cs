using System;

namespace DungeonInn.Domain.Combat
{
    public sealed class DamageSpec
    {
        public int Amount { get; }

        public DamageSpec(int amount)
        {
            Amount = Math.Max(0, amount);
        }
    }
}
