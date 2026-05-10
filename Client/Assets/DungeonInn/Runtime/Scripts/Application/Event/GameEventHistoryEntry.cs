using System;

namespace DungeonInn.Application.Event
{
    public readonly struct GameEventHistoryEntry
    {
        public int Sequence { get; }
        public int Day { get; }
        public int ScheduleTick { get; }
        public IGameEvent Event { get; }

        public GameEventHistoryEntry(int sequence, int day, int scheduleTick, IGameEvent gameEvent)
        {
            Sequence = sequence;
            Day = day;
            ScheduleTick = scheduleTick;
            Event = gameEvent ?? throw new ArgumentNullException(nameof(gameEvent));
        }
    }
}
