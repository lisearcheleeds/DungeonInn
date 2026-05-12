using System;
using Cysharp.Threading.Tasks;
using VContainer;

namespace DungeonInn.Application.GameLoop
{
    public sealed class ToggleGamePauseUseCase
    {
        readonly IGameClock gameClock;

        [Inject]
        public ToggleGamePauseUseCase(IGameClock gameClock)
        {
            this.gameClock = gameClock ?? throw new ArgumentNullException(nameof(gameClock));
        }

        public UniTask<GameTimeState> ExecuteAsync()
        {
            if (gameClock.IsPaused)
            {
                gameClock.Resume();
            }
            else
            {
                gameClock.Pause();
            }

            return UniTask.FromResult(new GameTimeState(
                gameClock.TotalScheduleTick,
                gameClock.ElapsedRealTimeSeconds,
                gameClock.ElapsedGameTimeSeconds,
                gameClock.TimeScale,
                gameClock.IsPaused));
        }
    }
}
