using DungeonInn.Domain.Actor;

namespace DungeonInn.Application.Factory
{
    public interface IAdventurerFactory
    {
        Actor Create(AdventurerCreateRequest request);
    }
}
