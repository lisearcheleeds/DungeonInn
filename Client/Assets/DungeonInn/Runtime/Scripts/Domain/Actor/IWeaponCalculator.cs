using DungeonInn.Domain.Item;

namespace DungeonInn.Domain.Actor
{
    public interface IWeaponCalculator
    {
        int CalculateAttack(ActorStats stats, EquipmentMaster weaponMaster, IActorBehavior behavior, int level);
    }
}
