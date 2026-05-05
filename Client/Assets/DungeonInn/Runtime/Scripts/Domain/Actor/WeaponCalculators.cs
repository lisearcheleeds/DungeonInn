using System;
using DungeonInn.Domain.Item;

namespace DungeonInn.Domain.Actor
{
    public sealed class SwordWeaponCalculator : IWeaponCalculator
    {
        public int CalculateAttack(ActorStats stats, EquipmentMaster weaponMaster, IActorBehavior behavior, int level)
        {
            return WeaponCalculatorMath.ClampAttack(stats.Strength * 3 + stats.Dexterity + level + WeaponCalculatorMath.GetWeaponAttack(weaponMaster));
        }
    }

    public sealed class BowWeaponCalculator : IWeaponCalculator
    {
        public int CalculateAttack(ActorStats stats, EquipmentMaster weaponMaster, IActorBehavior behavior, int level)
        {
            return WeaponCalculatorMath.ClampAttack(stats.Dexterity * 3 + stats.Strength + level + WeaponCalculatorMath.GetWeaponAttack(weaponMaster));
        }
    }

    public sealed class AxeWeaponCalculator : IWeaponCalculator
    {
        public int CalculateAttack(ActorStats stats, EquipmentMaster weaponMaster, IActorBehavior behavior, int level)
        {
            return WeaponCalculatorMath.ClampAttack(stats.Strength * 4 + level + WeaponCalculatorMath.GetWeaponAttack(weaponMaster));
        }
    }

    public sealed class ScytheWeaponCalculator : IWeaponCalculator
    {
        public int CalculateAttack(ActorStats stats, EquipmentMaster weaponMaster, IActorBehavior behavior, int level)
        {
            return WeaponCalculatorMath.ClampAttack(stats.Strength * 2 + stats.Dexterity * 2 + level + WeaponCalculatorMath.GetWeaponAttack(weaponMaster));
        }
    }

    public sealed class FistWeaponCalculator : IWeaponCalculator
    {
        public int CalculateAttack(ActorStats stats, EquipmentMaster weaponMaster, IActorBehavior behavior, int level)
        {
            return WeaponCalculatorMath.ClampAttack(stats.Strength * 2 + stats.Dexterity + level + WeaponCalculatorMath.GetWeaponAttack(weaponMaster));
        }
    }

    public sealed class ClawsWeaponCalculator : IWeaponCalculator
    {
        public int CalculateAttack(ActorStats stats, EquipmentMaster weaponMaster, IActorBehavior behavior, int level)
        {
            return WeaponCalculatorMath.ClampAttack(stats.Dexterity * 3 + stats.Strength * 2 + level + WeaponCalculatorMath.GetWeaponAttack(weaponMaster));
        }
    }

    public sealed class FangsWeaponCalculator : IWeaponCalculator
    {
        public int CalculateAttack(ActorStats stats, EquipmentMaster weaponMaster, IActorBehavior behavior, int level)
        {
            return WeaponCalculatorMath.ClampAttack(stats.Strength * 3 + stats.Constitution + level + WeaponCalculatorMath.GetWeaponAttack(weaponMaster));
        }
    }

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
    }

    static class WeaponCalculatorMath
    {
        public static int GetWeaponAttack(EquipmentMaster weaponMaster)
        {
            return weaponMaster == null ? 0 : weaponMaster.Attack + weaponMaster.Modifier;
        }

        public static int ClampAttack(int value)
        {
            return Math.Max(0, value);
        }
    }
}
