using R3;

namespace DungeonInn.Application.Event
{
    public interface IGameEventBus
    {
        void Publish(IGameEvent gameEvent);
        Observable<T> OnEvent<T>() where T : class, IGameEvent;
    }
}
