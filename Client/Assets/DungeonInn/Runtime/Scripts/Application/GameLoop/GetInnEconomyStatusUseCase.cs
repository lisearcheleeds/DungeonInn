using System;
using Cysharp.Threading.Tasks;
using VContainer;

namespace DungeonInn.Application.GameLoop
{
    public sealed class GetInnEconomyStatusUseCase
    {
        readonly IGameWorldState worldState;
        readonly IGameClock gameClock;
        readonly InnEconomyStatusCalculator calculator;

        [Inject]
        public GetInnEconomyStatusUseCase(
            IGameWorldState worldState,
            IGameClock gameClock,
            InnEconomyStatusCalculator calculator)
        {
            this.worldState = worldState ?? throw new ArgumentNullException(nameof(worldState));
            this.gameClock = gameClock ?? throw new ArgumentNullException(nameof(gameClock));
            this.calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
        }

        public UniTask<InnEconomyStatus> ExecuteAsync()
        {
            return UniTask.FromResult(calculator.Calculate(worldState, gameClock.CurrentDay));
        }
    }
}
