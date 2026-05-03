using System;

namespace DungeonInn.Domain.Character
{
    public class AdventurerCharacter : CharacterBase
    {
        public Guid Id { get; }
        public Satisfaction CurrentSatisfaction { get; private set; }
        public AdventurerState State { get; private set; }

        public AdventurerCharacter(Guid id, int maxHp, int attackPower, int defense, float moveSpeed, float attackSpeed)
            : base(maxHp, attackPower, defense, moveSpeed, attackSpeed)
        {
            Id = id;
            CurrentSatisfaction = new Satisfaction(1f);
            State = AdventurerState.Resting;
        }

        public void UpdateSatisfaction(Satisfaction satisfaction)
        {
            CurrentSatisfaction = satisfaction;
        }

        public void TransitionState(AdventurerState next)
        {
            State = next;
        }
    }

    public enum AdventurerState
    {
        Resting,
        TravelingToDungeon,
        ExploringDungeon,
        Returning,
    }
}
