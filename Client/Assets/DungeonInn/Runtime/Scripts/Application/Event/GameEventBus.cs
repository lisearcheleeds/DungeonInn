using System;
using R3;

namespace DungeonInn.Application.Event
{
    /// <summary>
    /// TODO: MessagePipeの導入の検討
    /// </summary>
    public sealed class GameEventBus : IGameEventBus, IDisposable
    {
        readonly Subject<IGameEvent> subject = new();

        public void Publish(IGameEvent gameEvent)
        {
            subject.OnNext(gameEvent);
        }

        public Observable<T> OnEvent<T>() where T : class, IGameEvent
        {
            return subject.Where(e => e is T).Select(e => (T)(object)e);
        }

        public void Dispose() => subject.Dispose();
    }
}
