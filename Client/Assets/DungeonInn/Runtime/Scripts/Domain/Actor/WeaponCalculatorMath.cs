using System;
using DungeonInn.Master;

namespace DungeonInn.Domain.Actor
{
    static class WeaponCalculatorMath
    {
        public static int GetWeaponAttack(WeaponMaster weaponMaster, EquipmentMaster weaponEquipmentMaster)
        {
            return weaponMaster == null ? 0 : weaponMaster.Attack;
        }

        public static int ClampAttack(int value)
        {
            return Math.Max(0, value);
        }
    }
}
