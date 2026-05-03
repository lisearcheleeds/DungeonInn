using DungeonInn.Domain.Character;
using DungeonInn.Domain.World;

namespace DungeonInn.Application.AI
{
    internal class RestingState : IAdventurerAIState
    {
        private readonly float restDuration;
        private float elapsed;

        public RestingState(float restDuration)
        {
            this.restDuration = restDuration;
        }

        public AdventurerState StateType => AdventurerState.Resting;

        public void Enter(AdventurerCharacter character)
        {
            elapsed = 0f;
        }

        public GridPosition? Tick(AdventurerCharacter character, float deltaTime)
        {
            elapsed += deltaTime;

            if (elapsed >= restDuration)
            {
                character.TransitionState(AdventurerState.TravelingToDungeon);
            }

            return null;
        }

        public void Exit(AdventurerCharacter character)
        {
        }
    }
}
