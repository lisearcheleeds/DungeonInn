using System;
using System.Collections.Generic;

namespace DungeonInn.Application.Event
{
    public sealed class BufferedEventPublisher : IEventPublisher
    {
        readonly IEventPublisher target;
        readonly List<IGameEvent> events = new();

        public BufferedEventPublisher(IEventPublisher target)
        {
            this.target = target ?? throw new ArgumentNullException(nameof(target));
        }

        public void Publish(IGameEvent gameEvent)
        {
            events.Add(gameEvent ?? throw new ArgumentNullException(nameof(gameEvent)));
        }

        public void Flush()
        {
            foreach (var gameEvent in events)
            {
                target.Publish(gameEvent);
            }

            events.Clear();
        }
    }
}
