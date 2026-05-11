using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Event;
using VContainer;

namespace DungeonInn.Application.GameLoop
{
    public sealed class GetInnEconomyReportUseCase
    {
        readonly IGameWorldStateReader worldState;
        readonly IGameEventHistoryReader historyReader;
        readonly InnEconomyStatusCalculator calculator;

        [Inject]
        public GetInnEconomyReportUseCase(
            IGameWorldStateReader worldState,
            IGameEventHistoryReader historyReader,
            InnEconomyStatusCalculator calculator)
        {
            this.worldState = worldState ?? throw new ArgumentNullException(nameof(worldState));
            this.historyReader = historyReader ?? throw new ArgumentNullException(nameof(historyReader));
            this.calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
        }

        public UniTask<InnEconomyReport> GetByDayAsync(int day)
        {
            return UniTask.FromResult(calculator.CalculateReport(
                worldState,
                day,
                day,
                historyReader.GetByDay(day)));
        }

        public UniTask<InnEconomyReport> GetByDayRangeAsync(int startDay, int endDay)
        {
            return UniTask.FromResult(calculator.CalculateReport(
                worldState,
                startDay,
                endDay,
                historyReader.GetByDayRange(startDay, endDay)));
        }
    }
}
