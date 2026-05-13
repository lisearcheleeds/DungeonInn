using DungeonInn.Application.GameLoop;
using System;
using Cysharp.Threading.Tasks;
using VContainer;

namespace DungeonInn.Application.GameLoop
{
    public sealed class GetGameTimeStateUseCase
    {
        readonly IGameClock gameClock;

        [Inject]
        public GetGameTimeStateUseCase(IGameClock gameClock)
        {
            this.gameClock = gameClock ?? throw new ArgumentNullException(nameof(gameClock));
        }

        public UniTask<GameTimeState> ExecuteAsync()
        {
            return UniTask.FromResult(new GameTimeState(
                gameClock.TotalScheduleTick,
                gameClock.ElapsedRealTimeSeconds,
                gameClock.ElapsedGameTimeSeconds,
                gameClock.TimeScale,
                gameClock.IsPaused));
        }
    }
}
