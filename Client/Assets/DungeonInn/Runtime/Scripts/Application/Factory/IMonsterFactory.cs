using DungeonInn.Domain.Actor;

namespace DungeonInn.Application.Factory
{
    public interface IMonsterFactory
    {
        Actor Create(MonsterCreateRequest request);
    }
}
