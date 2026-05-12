using DungeonInn.Domain.Actor;

namespace DungeonInn.Application.Factory
{
    public interface IActorFactory
    {
        Actor Create(ActorFactoryRequest request);
    }
}
