using R3;

namespace DungeonInn.Application.Event
{
    public interface IEventPublisher
    {
        void Publish(IGameEvent gameEvent);
    }

    public interface IEventSubscriber
    {
        Observable<T> OnEvent<T>() where T : class, IGameEvent;
    }

    public interface IGameEventBus : IEventPublisher, IEventSubscriber
    {
    }
}
