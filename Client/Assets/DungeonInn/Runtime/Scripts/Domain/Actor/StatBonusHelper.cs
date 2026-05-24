using System.Collections.Generic;
using DungeonInn.Domain.Item;

namespace DungeonInn.Domain.Actor
{
    static class StatBonusHelper
    {
        public static int GetSum(IReadOnlyList<StatBonus> bonuses, StatType statType)
        {
            if (bonuses == null)
            {
                return 0;
            }

            var sum = 0;
            foreach (var bonus in bonuses)
            {
                if (bonus.StatType == statType)
                {
                    sum += bonus.Amount;
                }
            }

            return sum;
        }
    }
}
