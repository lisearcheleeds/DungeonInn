using System;
using Cysharp.Threading.Tasks;
using VContainer;

namespace DungeonInn.Application.GameLoop
{
    public sealed class GetInnEconomyStatusUseCase
    {
        readonly IGameWorldStateReader worldState;
        readonly IGameClock gameClock;
        readonly InnEconomyStatisticsService statisticsService;
        readonly InnEconomyStatusCalculator calculator;

        [Inject]
        public GetInnEconomyStatusUseCase(
            IGameWorldStateReader worldState,
            IGameClock gameClock,
            InnEconomyStatisticsService statisticsService,
            InnEconomyStatusCalculator calculator)
        {
            this.worldState = worldState ?? throw new ArgumentNullException(nameof(worldState));
            this.gameClock = gameClock ?? throw new ArgumentNullException(nameof(gameClock));
            this.statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
            this.calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
        }

        public UniTask<InnEconomyStatus> ExecuteAsync()
        {
            return UniTask.FromResult(calculator.Calculate(
                worldState,
                gameClock.CurrentDay,
                statisticsService.GetByDay(gameClock.CurrentDay)));
        }
    }
}
