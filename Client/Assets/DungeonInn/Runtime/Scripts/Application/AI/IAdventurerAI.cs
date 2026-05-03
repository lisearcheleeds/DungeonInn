using DungeonInn.Domain.Character;
using DungeonInn.Domain.World;

namespace DungeonInn.Application.AI
{
    public interface IAdventurerAI
    {
        void OnStateEnter(AdventurerCharacter character, AdventurerState state);
        GridPosition? Tick(AdventurerCharacter character, float deltaTime);
        void OnStateExit(AdventurerCharacter character, AdventurerState state);
    }
}
