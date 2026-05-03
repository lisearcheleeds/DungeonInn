using DungeonInn.Domain.Character;
using DungeonInn.Domain.World;

namespace DungeonInn.Application.AI
{
    public interface IAdventurerAIState
    {
        AdventurerState StateType { get; }
        void Enter(AdventurerCharacter character);
        GridPosition? Tick(AdventurerCharacter character, float deltaTime);
        void Exit(AdventurerCharacter character);
    }
}
