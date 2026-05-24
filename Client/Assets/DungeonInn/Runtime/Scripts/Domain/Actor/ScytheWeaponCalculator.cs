using System.Collections.Generic;
using DungeonInn.Domain.Item;
using DungeonInn.Master;

namespace DungeonInn.Domain.Actor
{
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
}
