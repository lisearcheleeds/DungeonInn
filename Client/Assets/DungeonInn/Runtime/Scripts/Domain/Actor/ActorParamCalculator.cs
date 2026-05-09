using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Master;

namespace DungeonInn.Domain.Actor
{
    public sealed class ActorParamCalculator
    {
        public ActorParams Calculate(
            ActorStats stats,
            IReadOnlyList<EquipmentMaster> equipmentMasters,
            IActorBehavior behavior,
            int level)
        {
            if (stats == null)
            {
                throw new ArgumentNullException(nameof(stats));
            }

            if (behavior == null)
            {
                throw new ArgumentNullException(nameof(behavior));
            }

            if (level < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            var equipment = equipmentMasters ?? Array.Empty<EquipmentMaster>();
            var equipmentDefense = equipment.Sum(x => x.Defense);

            var bonuses = CollectBonuses(equipment);
            var str = stats.Strength + GetBonusSum(bonuses, StatType.Strength);
            var dex = stats.Dexterity + GetBonusSum(bonuses, StatType.Dexterity);
            var con = stats.Constitution + GetBonusSum(bonuses, StatType.Constitution);
            var intel = stats.Intelligence + GetBonusSum(bonuses, StatType.Intelligence);
            var wis = stats.Wisdom + GetBonusSum(bonuses, StatType.Wisdom);

            return new ActorParams(
                con * 10 + str * 2 + level * 5 + equipmentDefense,
                intel * 5 + wis * 5 + level * 2,
                100 + dex * 2,
                con * 3 + wis + equipmentDefense,
                dex * 2 + wis * 2 + intel + level,
                str + dex + intel);
        }

        static List<StatBonus> CollectBonuses(IReadOnlyList<EquipmentMaster> equipment)
        {
            var result = new List<StatBonus>();
            foreach (var equipmentMaster in equipment)
            {
                result.AddRange(equipmentMaster.StatBonuses);
            }

            return result;
        }

        static int GetBonusSum(List<StatBonus> bonuses, StatType statType)
        {
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
