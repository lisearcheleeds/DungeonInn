using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Event;

namespace DungeonInn.Application.GameLoop
{
    public sealed class GetGameEventHistoryUseCase
    {
        readonly IGameEventHistoryReader historyReader;

        public GetGameEventHistoryUseCase(IGameEventHistoryReader historyReader)
        {
            this.historyReader = historyReader ?? throw new ArgumentNullException(nameof(historyReader));
        }

        public UniTask<IReadOnlyList<GameEventHistoryEntry>> GetRecentAsync(int count)
        {
            return UniTask.FromResult(historyReader.GetRecent(count));
        }

        public UniTask<IReadOnlyList<GameEventHistoryEntry>> GetByDayAsync(int day)
        {
            return UniTask.FromResult(historyReader.GetByDay(day));
        }

        public UniTask<IReadOnlyList<GameEventHistoryEntry>> GetByTickRangeAsync(int startTick, int endTick)
        {
            return UniTask.FromResult(historyReader.GetByTickRange(startTick, endTick));
        }

        public UniTask<IReadOnlyList<GameEventHistoryEntry>> GetByDayRangeAsync(int startDay, int endDay)
        {
            return UniTask.FromResult(historyReader.GetByDayRange(startDay, endDay));
        }
    }
}
