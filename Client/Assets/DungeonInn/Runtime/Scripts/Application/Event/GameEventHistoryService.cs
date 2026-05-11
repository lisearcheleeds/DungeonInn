using System;
using System.Collections.Generic;
using DungeonInn.Application.GameLoop;

namespace DungeonInn.Application.Event
{
    public sealed class GameEventHistoryService : IGameEventHistoryReader, IGameEventHistoryRecorder
    {
        const int Capacity = 4096;

        readonly IGameClock gameClock;
        readonly Queue<GameEventHistoryEntry> entries = new();
        int sequence;

        public GameEventHistoryService(IGameClock gameClock)
        {
            this.gameClock = gameClock ?? throw new ArgumentNullException(nameof(gameClock));
        }

        public void Record(IGameEvent gameEvent)
        {
            if (gameEvent == null)
            {
                throw new ArgumentNullException(nameof(gameEvent));
            }

            if (entries.Count >= Capacity)
            {
                entries.Dequeue();
            }

            entries.Enqueue(new GameEventHistoryEntry(
                sequence++,
                gameClock.CurrentDay,
                gameClock.CurrentScheduleTick,
                gameEvent));
        }

        public IReadOnlyList<GameEventHistoryEntry> GetRecent(int count)
        {
            if (count <= 0 || entries.Count == 0)
            {
                return Array.Empty<GameEventHistoryEntry>();
            }

            var source = entries.ToArray();
            var startIndex = Math.Max(0, source.Length - count);
            var result = new GameEventHistoryEntry[source.Length - startIndex];
            Array.Copy(source, startIndex, result, 0, result.Length);
            return result;
        }

        public IReadOnlyList<GameEventHistoryEntry> GetByDay(int day)
        {
            var result = new List<GameEventHistoryEntry>();
            foreach (var entry in entries)
            {
                if (entry.Day == day)
                {
                    result.Add(entry);
                }
            }

            return result;
        }

        public IReadOnlyList<GameEventHistoryEntry> GetByDayRange(int startDay, int endDay)
        {
            var result = new List<GameEventHistoryEntry>();
            foreach (var entry in entries)
            {
                if (startDay <= entry.Day && entry.Day <= endDay)
                {
                    result.Add(entry);
                }
            }

            return result;
        }
    }
}
