using DungeonInn.Domain.Item;

namespace DungeonInn.Domain.Actor
{
    public interface IWeaponCalculator
    {
        int CalculateAttack(ActorStats stats, EquipmentSpec weaponSpec, IActorBehavior behavior, int level);
    }
}
