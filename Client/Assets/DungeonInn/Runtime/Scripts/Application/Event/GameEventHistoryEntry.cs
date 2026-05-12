using System;

namespace DungeonInn.Application.Event
{
    public readonly struct GameEventHistoryEntry
    {
        public int Sequence { get; }
        public int OccurredAtTick { get; }
        public int ScheduleTick => OccurredAtTick;
        public IGameEvent Event { get; }

        public GameEventHistoryEntry(
            int sequence,
            int occurredAtTick,
            IGameEvent gameEvent)
        {
            Sequence = sequence;
            OccurredAtTick = occurredAtTick;
            Event = gameEvent ?? throw new ArgumentNullException(nameof(gameEvent));
        }
    }
}
