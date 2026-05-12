using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Guild;
using VContainer;

namespace DungeonInn.Application.GameLoop
{
    public sealed class GetInnEconomyReportUseCase
    {
        readonly InnDailyReportStore reportStore;

        [Inject]
        public GetInnEconomyReportUseCase(InnDailyReportStore reportStore)
        {
            this.reportStore = reportStore ?? throw new ArgumentNullException(nameof(reportStore));
        }

        public UniTask<InnDailyReport?> TryGetByDayAsync(int day)
        {
            if (!reportStore.TryGet(day, out var report))
            {
                return UniTask.FromResult<InnDailyReport?>(null);
            }

            return UniTask.FromResult<InnDailyReport?>(report);
        }

        public UniTask<IReadOnlyList<InnDailyReport>> GetByDayRangeAsync(int startDay, int endDay)
        {
            return UniTask.FromResult(reportStore.GetRange(startDay, endDay));
        }
    }
}
