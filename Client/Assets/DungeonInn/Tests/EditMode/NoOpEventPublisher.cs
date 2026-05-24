using DungeonInn.Application.Event;

namespace DungeonInn.Tests.EditMode
{
    public sealed class NoOpEventPublisher : IEventPublisher
    {
        public void Publish(IGameEvent gameEvent)
        {
        }
    }
}

