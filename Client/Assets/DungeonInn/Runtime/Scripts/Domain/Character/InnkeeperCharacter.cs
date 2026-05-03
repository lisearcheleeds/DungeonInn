namespace DungeonInn.Domain.Character
{
    public class InnkeeperCharacter : CharacterBase
    {
        public InnkeeperCharacter(int maxHp, int attackPower, int defense, float moveSpeed, float attackSpeed)
            : base(maxHp, attackPower, defense, moveSpeed, attackSpeed)
        {
        }
    }
}
