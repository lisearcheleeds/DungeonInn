using System;
using System.Collections.Generic;
using DungeonInn.Master;

namespace DungeonInn.Domain.Actor
{
    public static class ActorParamCalculator
    {
        public static ActorParams Calculate(
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
            var equipmentDefense = 0;
            var strengthBonus = 0;
            var dexterityBonus = 0;
            var constitutionBonus = 0;
            var intelligenceBonus = 0;
            var wisdomBonus = 0;

            for (var i = 0; i < equipment.Count; i++)
            {
                var equipmentMaster = equipment[i];
                equipmentDefense += equipmentMaster.Defense;
                AddStatBonuses(
                    equipmentMaster.StatBonuses,
                    ref strengthBonus,
                    ref dexterityBonus,
                    ref constitutionBonus,
                    ref intelligenceBonus,
                    ref wisdomBonus);
            }

            var str = stats.Strength + strengthBonus;
            var dex = stats.Dexterity + dexterityBonus;
            var con = stats.Constitution + constitutionBonus;
            var intel = stats.Intelligence + intelligenceBonus;
            var wis = stats.Wisdom + wisdomBonus;

            return new ActorParams(
                con * 10 + str * 2 + level * 5 + equipmentDefense,
                intel * 5 + wis * 5 + level * 2,
                100 + dex * 2,
                con * 3 + wis + equipmentDefense,
                dex * 2 + wis * 2 + intel + level,
                str + dex + intel);
        }

        static void AddStatBonuses(
            IReadOnlyList<StatBonus> bonuses,
            ref int strengthBonus,
            ref int dexterityBonus,
            ref int constitutionBonus,
            ref int intelligenceBonus,
            ref int wisdomBonus)
        {
            for (var i = 0; i < bonuses.Count; i++)
            {
                var bonus = bonuses[i];
                switch (bonus.StatType)
                {
                    case StatType.Strength:
                        strengthBonus += bonus.Amount;
                        break;
                    case StatType.Dexterity:
                        dexterityBonus += bonus.Amount;
                        break;
                    case StatType.Constitution:
                        constitutionBonus += bonus.Amount;
                        break;
                    case StatType.Intelligence:
                        intelligenceBonus += bonus.Amount;
                        break;
                    case StatType.Wisdom:
                        wisdomBonus += bonus.Amount;
                        break;
                }
            }
        }
    }
}
