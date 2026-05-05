using DungeonInn.Domain.Actor;

namespace DungeonInn.Application.Factory
{
    public interface IActorFactory
    {
        Actor CreateActor(ActorCreateRequest request);
        Actor CreateMonster(MonsterCreateRequest request);
    }
}
