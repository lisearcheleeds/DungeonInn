using DungeonInn.Domain.Character;
using DungeonInn.Domain.World;

namespace DungeonInn.Application.AI
{
    internal class ReturningState : IAdventurerAIState
    {
        private readonly GridPosition innEntrance;

        public ReturningState(GridPosition innEntrance)
        {
            this.innEntrance = innEntrance;
        }

        public AdventurerState StateType => AdventurerState.Returning;

        public void Enter(AdventurerCharacter character)
        {
        }

        public GridPosition? Tick(AdventurerCharacter character, float deltaTime)
        {
            return innEntrance;
        }

        public void Exit(AdventurerCharacter character)
        {
        }
    }
}
