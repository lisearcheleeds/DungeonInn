using System.Collections.Generic;
using DungeonInn.Domain.Item;
using DungeonInn.Master;

namespace DungeonInn.Domain.Actor
{
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
}
