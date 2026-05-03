using DungeonInn.Domain.World;

namespace DungeonInn.Domain.Character
{
    public class MonsterCharacter : CharacterBase
    {
        public int FloorIndex { get; }
        public GridPosition PatrolTarget { get; private set; }

        public MonsterCharacter(int floorIndex, int maxHp, int attackPower, int defense, float moveSpeed, float attackSpeed)
            : base(maxHp, attackPower, defense, moveSpeed, attackSpeed)
        {
            FloorIndex = floorIndex;
        }

        public void SetPatrolTarget(GridPosition target)
        {
            PatrolTarget = target;
        }
    }
}
