using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Domain.Item;

namespace DungeonInn.Domain.Actor
{
    public sealed class ActorParamCalculator
    {
        public ActorParams Calculate(
            ActorStats stats,
            IReadOnlyList<EquipmentSpec> equipmentSpecs,
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

            var equipment = equipmentSpecs ?? Array.Empty<EquipmentSpec>();
            var equipmentDefense = equipment.Sum(x => x.Defense);
            var equipmentModifier = equipment.Sum(x => x.Modifier);

            return new ActorParams(
                stats.Constitution * 10 + stats.Strength * 2 + level * 5 + equipmentDefense,
                stats.Intelligence * 5 + stats.Wisdom * 5 + level * 2,
                100 + stats.Dexterity * 2,
                stats.Constitution * 3 + stats.Wisdom + equipmentDefense,
                stats.Dexterity * 2 + stats.Wisdom * 2 + stats.Intelligence + level,
                stats.Strength + stats.Dexterity + stats.Intelligence + Math.Max(0, equipmentModifier));
        }
    }
}
