using DungeonInn.Domain.Actor;
using DungeonInn.Master;
using ActorEntity = DungeonInn.Domain.Actor.Actor;

namespace DungeonInn.Domain.Combat
{
    public interface IWeaponCombatCalculator
    {
        WeaponCombatParams Calculate(ActorEntity actor, WeaponMaster weaponMaster);
    }
}
