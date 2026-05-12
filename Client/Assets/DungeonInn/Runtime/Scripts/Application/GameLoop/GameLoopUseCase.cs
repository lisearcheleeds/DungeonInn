using System;
using Cysharp.Threading.Tasks;
using VContainer;

namespace DungeonInn.Application.GameLoop
{
    public sealed class GameLoopUseCase : IGameLoopUseCase
    {
        readonly IGameClock gameClock;

        [Inject]
        public GameLoopUseCase(IGameClock gameClock)
        {
            this.gameClock = gameClock ?? throw new ArgumentNullException(nameof(gameClock));
        }

        public UniTask<GameLoopTickResult> ExecuteAsync(GameLoopTickRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var advanceResult = gameClock.Advance(request.UnscaledDeltaTimeSeconds);

            return UniTask.FromResult(new GameLoopTickResult(
                gameClock.TotalScheduleTick,
                advanceResult.AdvancedScheduleTicks,
                advanceResult.CompletedDays,
                gameClock.ElapsedRealTimeSeconds,
                gameClock.ElapsedGameTimeSeconds,
                gameClock.TimeScale,
                gameClock.IsPaused));
        }
    }
}
