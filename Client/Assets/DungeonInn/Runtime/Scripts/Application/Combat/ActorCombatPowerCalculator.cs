using System;
using DungeonInn.Domain.Actor;
using DungeonInn.Master;

namespace DungeonInn.Application.Combat
{
    public sealed class ActorCombatPowerCalculator
    {
        public int Calculate(Actor actor)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            return CalculateStats(actor.Stats)
                + actor.Equipment.TotalStatBonusAmount
                + actor.Equipment.EquippedWeaponAttack
                + actor.Equipment.TotalDefense;
        }

        public int Calculate(ActorArchetypeMaster archetypeMaster)
        {
            if (archetypeMaster == null)
            {
                throw new ArgumentNullException(nameof(archetypeMaster));
            }

            return CalculateStats(archetypeMaster.BaseStats);
        }

        static int CalculateStats(ActorStats stats)
        {
            return stats.Strength
                + stats.Dexterity
                + stats.Constitution
                + stats.Intelligence
                + stats.Wisdom
                + stats.Charisma;
        }
    }
}
