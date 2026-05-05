using DungeonInn.Master;

namespace DungeonInn.Domain.Combat
{
    public interface IWeaponCombatCalculator
    {
        WeaponCombatParams Calculate(IWeaponCombatSource source, WeaponMaster weaponMaster);
    }
}
