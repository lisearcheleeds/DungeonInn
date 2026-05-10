using System.Collections.Generic;

namespace DungeonInn.Application.Event
{
    public interface IGameEventHistoryReader
    {
        IReadOnlyList<GameEventHistoryEntry> GetRecent(int count);
        IReadOnlyList<GameEventHistoryEntry> GetByDay(int day);
    }
}
