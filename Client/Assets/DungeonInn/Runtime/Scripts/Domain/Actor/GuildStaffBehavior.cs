using System;
using System.Collections.Generic;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Item;

namespace DungeonInn.Domain.Actor
{
    public sealed class GuildStaffBehavior : IActorBehavior
    {
        public IReadOnlyList<ItemStack> Salary { get; }

        public GuildStaffBehavior(IReadOnlyList<ItemStack> salary)
        {
            Salary = salary ?? throw new ArgumentNullException(nameof(salary));
        }

        public int CalculateFacilityPoint(Actor actor, FacilityType facilityType)
        {
            switch (facilityType)
            {
                case FacilityType.Inn:
                    return actor.Stats.Constitution * 3 + actor.Stats.Wisdom * 2 + actor.Stats.Charisma;
                case FacilityType.Tavern:
                    return actor.Stats.Charisma * 4 + actor.Stats.Wisdom * 2 + actor.Stats.Dexterity;
                case FacilityType.GeneralStore:
                    return actor.Stats.Intelligence * 3 + actor.Stats.Charisma * 2 + actor.Stats.Wisdom;
                case FacilityType.EquipmentShop:
                    return actor.Stats.Strength * 2 + actor.Stats.Dexterity * 2 + actor.Stats.Intelligence * 2;
                default:
                    throw new ArgumentOutOfRangeException(nameof(facilityType));
            }
        }
    }
}
