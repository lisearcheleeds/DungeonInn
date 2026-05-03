using DungeonInn.Domain.Character;
using DungeonInn.Domain.World;

namespace DungeonInn.Application.AI
{
    internal class TravelingState : IAdventurerAIState
    {
        private readonly GridPosition dungeonEntrance;

        public TravelingState(GridPosition dungeonEntrance)
        {
            this.dungeonEntrance = dungeonEntrance;
        }

        public AdventurerState StateType => AdventurerState.TravelingToDungeon;

        public void Enter(AdventurerCharacter character)
        {
        }

        public GridPosition? Tick(AdventurerCharacter character, float deltaTime)
        {
            return dungeonEntrance;
        }

        public void Exit(AdventurerCharacter character)
        {
        }
    }
}
