using System;

namespace DungeonInn.Domain.Character
{
    public abstract class CharacterBase
    {
        public int Hp { get; protected set; }
        public int MaxHp { get; }
        public int AttackPower { get; }
        public int Defense { get; }
        public float MoveSpeed { get; }
        public float AttackSpeed { get; }
        public bool IsAlive => Hp > 0;

        protected CharacterBase(int maxHp, int attackPower, int defense, float moveSpeed, float attackSpeed)
        {
            if (maxHp <= 0) throw new ArgumentOutOfRangeException(nameof(maxHp));

            MaxHp = maxHp;
            Hp = maxHp;
            AttackPower = attackPower;
            Defense = defense;
            MoveSpeed = moveSpeed;
            AttackSpeed = attackSpeed;
        }

        public void TakeDamage(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Hp = Math.Max(0, Hp - amount);
        }

        public void Heal(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Hp = Math.Min(MaxHp, Hp + amount);
        }
    }
}
