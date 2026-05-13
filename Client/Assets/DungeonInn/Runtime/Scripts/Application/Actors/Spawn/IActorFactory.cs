using DungeonInn.Domain.Actor;

namespace DungeonInn.Application.Actors.Spawn
{
    public interface IActorFactory
    {
        Actor Create(ActorFactoryRequest request);
    }
}
