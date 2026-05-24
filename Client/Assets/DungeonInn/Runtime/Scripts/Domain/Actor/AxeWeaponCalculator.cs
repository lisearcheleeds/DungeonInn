using System.Collections.Generic;
using DungeonInn.Domain.Item;
using DungeonInn.Master;

namespace DungeonInn.Domain.Actor
{
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
}
