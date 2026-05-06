using System;
using DungeonInn.Domain.Item;
using DungeonInn.Master;

namespace DungeonInn.Domain.Combat
{
    public static class WeaponCombatCalculatorFactory
    {
        public static IWeaponCombatCalculator Create(WeaponType weaponType)
        {
            return Create(WeaponTypeCombatMasterCatalog.Get(weaponType));
        }

        public static IWeaponCombatCalculator Create(WeaponTypeCombatMaster weaponTypeCombatMaster)
        {
            if (weaponTypeCombatMaster == null)
            {
                throw new ArgumentNullException(nameof(weaponTypeCombatMaster));
            }

            return new DirectWeaponCombatCalculator(weaponTypeCombatMaster);
        }
    }
}
