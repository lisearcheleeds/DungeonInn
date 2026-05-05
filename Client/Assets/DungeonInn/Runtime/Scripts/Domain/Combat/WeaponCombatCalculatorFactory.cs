using System;
using DungeonInn.Domain.Item;

namespace DungeonInn.Domain.Combat
{
    public static class WeaponCombatCalculatorFactory
    {
        public static IWeaponCombatCalculator Create(WeaponType weaponType)
        {
            switch (weaponType)
            {
                case WeaponType.Sword:
                    return new DirectWeaponCombatCalculator(2.0f, 1.2f);
                case WeaponType.Bow:
                    return new DirectWeaponCombatCalculator(20.0f, 1.5f);
                case WeaponType.Axe:
                    return new DirectWeaponCombatCalculator(2.0f, 1.8f);
                case WeaponType.Scythe:
                    return new DirectWeaponCombatCalculator(3.0f, 1.6f);
                case WeaponType.Claws:
                    return new DirectWeaponCombatCalculator(1.5f, 0.9f);
                case WeaponType.Fangs:
                    return new DirectWeaponCombatCalculator(1.5f, 1.0f);
                case WeaponType.None:
                case WeaponType.Fist:
                    return new DirectWeaponCombatCalculator(1.5f, 1.0f);
                default:
                    throw new ArgumentOutOfRangeException(nameof(weaponType));
            }
        }
    }
}
