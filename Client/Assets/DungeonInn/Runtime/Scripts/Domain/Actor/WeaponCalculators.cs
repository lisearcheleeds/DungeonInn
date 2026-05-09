using System;
using System.Collections.Generic;
using DungeonInn.Domain.Item;
using DungeonInn.Master;

namespace DungeonInn.Domain.Actor
{
    public sealed class SwordWeaponCalculator : IWeaponCalculator
    {
        public int CalculateAttack(
            ActorStats stats,
            WeaponMaster weaponMaster,
            EquipmentMaster weaponEquipmentMaster,
            IActorBehavior behavior,
            int level,
            IReadOnlyList<StatBonus> allEquipmentBonuses)
        {
            var str = stats.Strength + StatBonusHelper.GetSum(allEquipmentBonuses, StatType.Strength);
            var dex = stats.Dexterity + StatBonusHelper.GetSum(allEquipmentBonuses, StatType.Dexterity);
            return WeaponCalculatorMath.ClampAttack(str * 3 + dex + level + WeaponCalculatorMath.GetWeaponAttack(weaponMaster, weaponEquipmentMaster));
        }
    }

    public sealed class BowWeaponCalculator : IWeaponCalculator
    {
        public int CalculateAttack(
            ActorStats stats,
            WeaponMaster weaponMaster,
            EquipmentMaster weaponEquipmentMaster,
            IActorBehavior behavior,
            int level,
            IReadOnlyList<StatBonus> allEquipmentBonuses)
        {
            var str = stats.Strength + StatBonusHelper.GetSum(allEquipmentBonuses, StatType.Strength);
            var dex = stats.Dexterity + StatBonusHelper.GetSum(allEquipmentBonuses, StatType.Dexterity);
            return WeaponCalculatorMath.ClampAttack(dex * 3 + str + level + WeaponCalculatorMath.GetWeaponAttack(weaponMaster, weaponEquipmentMaster));
        }
    }

    public sealed class AxeWeaponCalculator : IWeaponCalculator
    {
        public int CalculateAttack(
            ActorStats stats,
            WeaponMaster weaponMaster,
            EquipmentMaster weaponEquipmentMaster,
            IActorBehavior behavior,
            int level,
            IReadOnlyList<StatBonus> allEquipmentBonuses)
        {
            var str = stats.Strength + StatBonusHelper.GetSum(allEquipmentBonuses, StatType.Strength);
            return WeaponCalculatorMath.ClampAttack(str * 4 + level + WeaponCalculatorMath.GetWeaponAttack(weaponMaster, weaponEquipmentMaster));
        }
    }

    public sealed class ScytheWeaponCalculator : IWeaponCalculator
    {
        public int CalculateAttack(
            ActorStats stats,
            WeaponMaster weaponMaster,
            EquipmentMaster weaponEquipmentMaster,
            IActorBehavior behavior,
            int level,
            IReadOnlyList<StatBonus> allEquipmentBonuses)
        {
            var str = stats.Strength + StatBonusHelper.GetSum(allEquipmentBonuses, StatType.Strength);
            var dex = stats.Dexterity + StatBonusHelper.GetSum(allEquipmentBonuses, StatType.Dexterity);
            return WeaponCalculatorMath.ClampAttack(str * 2 + dex * 2 + level + WeaponCalculatorMath.GetWeaponAttack(weaponMaster, weaponEquipmentMaster));
        }
    }

    public sealed class FistWeaponCalculator : IWeaponCalculator
    {
        public int CalculateAttack(
            ActorStats stats,
            WeaponMaster weaponMaster,
            EquipmentMaster weaponEquipmentMaster,
            IActorBehavior behavior,
            int level,
            IReadOnlyList<StatBonus> allEquipmentBonuses)
        {
            var str = stats.Strength + StatBonusHelper.GetSum(allEquipmentBonuses, StatType.Strength);
            var dex = stats.Dexterity + StatBonusHelper.GetSum(allEquipmentBonuses, StatType.Dexterity);
            return WeaponCalculatorMath.ClampAttack(str * 2 + dex + level + WeaponCalculatorMath.GetWeaponAttack(weaponMaster, weaponEquipmentMaster));
        }
    }

    public sealed class ClawsWeaponCalculator : IWeaponCalculator
    {
        public int CalculateAttack(
            ActorStats stats,
            WeaponMaster weaponMaster,
            EquipmentMaster weaponEquipmentMaster,
            IActorBehavior behavior,
            int level,
            IReadOnlyList<StatBonus> allEquipmentBonuses)
        {
            var str = stats.Strength + StatBonusHelper.GetSum(allEquipmentBonuses, StatType.Strength);
            var dex = stats.Dexterity + StatBonusHelper.GetSum(allEquipmentBonuses, StatType.Dexterity);
            return WeaponCalculatorMath.ClampAttack(dex * 3 + str * 2 + level + WeaponCalculatorMath.GetWeaponAttack(weaponMaster, weaponEquipmentMaster));
        }
    }

    public sealed class FangsWeaponCalculator : IWeaponCalculator
    {
        public int CalculateAttack(
            ActorStats stats,
            WeaponMaster weaponMaster,
            EquipmentMaster weaponEquipmentMaster,
            IActorBehavior behavior,
            int level,
            IReadOnlyList<StatBonus> allEquipmentBonuses)
        {
            var str = stats.Strength + StatBonusHelper.GetSum(allEquipmentBonuses, StatType.Strength);
            var con = stats.Constitution + StatBonusHelper.GetSum(allEquipmentBonuses, StatType.Constitution);
            return WeaponCalculatorMath.ClampAttack(str * 3 + con + level + WeaponCalculatorMath.GetWeaponAttack(weaponMaster, weaponEquipmentMaster));
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

    static class StatBonusHelper
    {
        public static int GetSum(IReadOnlyList<StatBonus> bonuses, StatType statType)
        {
            if (bonuses == null)
            {
                return 0;
            }

            var sum = 0;
            foreach (var bonus in bonuses)
            {
                if (bonus.StatType == statType)
                {
                    sum += bonus.Amount;
                }
            }

            return sum;
        }
    }
}
