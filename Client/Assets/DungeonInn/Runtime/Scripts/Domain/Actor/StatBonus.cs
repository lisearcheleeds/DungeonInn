using System;

namespace DungeonInn.Domain.Actor
{
    public readonly struct StatBonus
    {
        public StatType StatType { get; }
        public int Amount { get; }

        public StatBonus(StatType statType, int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            StatType = statType;
            Amount = amount;
        }
    }
}
