using System;
using System.Collections.Generic;
using DungeonInn.Domain.Item;

namespace DungeonInn.Domain.Actor
{
    public static class WeaponCalculatorFactory
    {
        public static IWeaponCalculator Create(WeaponType weaponType)
        {
            switch (weaponType)
            {
                case WeaponType.Sword:
                    return new SwordWeaponCalculator();
                case WeaponType.Bow:
                    return new BowWeaponCalculator();
                case WeaponType.Axe:
                    return new AxeWeaponCalculator();
                case WeaponType.Scythe:
                    return new ScytheWeaponCalculator();
                case WeaponType.Claws:
                    return new ClawsWeaponCalculator();
                case WeaponType.Fangs:
                    return new FangsWeaponCalculator();
                case WeaponType.None:
                case WeaponType.Fist:
                    return new FistWeaponCalculator();
                default:
                    throw new ArgumentOutOfRangeException(nameof(weaponType));
            }
        }

        public static IReadOnlyList<(StatType stat, int weight)> GetStatWeights(WeaponType weaponType)
        {
            switch (weaponType)
            {
                case WeaponType.Sword:
                    return new[] { (StatType.Strength, 3), (StatType.Dexterity, 1) };
                case WeaponType.Bow:
                    return new[] { (StatType.Dexterity, 3), (StatType.Strength, 1) };
                case WeaponType.Axe:
                    return new[] { (StatType.Strength, 4) };
                case WeaponType.Scythe:
                    return new[] { (StatType.Strength, 2), (StatType.Dexterity, 2) };
                case WeaponType.Claws:
                    return new[] { (StatType.Dexterity, 3), (StatType.Strength, 2) };
                case WeaponType.Fangs:
                    return new[] { (StatType.Strength, 3), (StatType.Constitution, 1) };
                case WeaponType.None:
                case WeaponType.Fist:
                    return new[] { (StatType.Strength, 2), (StatType.Dexterity, 1) };
                default:
                    throw new ArgumentOutOfRangeException(nameof(weaponType));
            }
        }
    }
}
