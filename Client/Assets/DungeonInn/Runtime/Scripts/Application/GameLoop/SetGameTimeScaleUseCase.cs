using DungeonInn.Application.GameLoop;
using System;
using Cysharp.Threading.Tasks;
using VContainer;

namespace DungeonInn.Application.GameLoop
{
    public sealed class SetGameTimeScaleUseCase
    {
        readonly IGameClock gameClock;

        [Inject]
        public SetGameTimeScaleUseCase(IGameClock gameClock)
        {
            this.gameClock = gameClock ?? throw new ArgumentNullException(nameof(gameClock));
        }

        public UniTask ExecuteAsync(float timeScale)
        {
            gameClock.SetTimeScale(timeScale);
            return UniTask.CompletedTask;
        }
    }
}
