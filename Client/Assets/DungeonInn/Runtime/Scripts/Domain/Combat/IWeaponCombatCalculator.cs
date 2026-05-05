using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Item;
using ActorEntity = DungeonInn.Domain.Actor.Actor;

namespace DungeonInn.Domain.Combat
{
    public interface IWeaponCombatCalculator
    {
        WeaponCombatParams Calculate(ActorEntity actor, EquipmentMaster weaponMaster);
    }
}
