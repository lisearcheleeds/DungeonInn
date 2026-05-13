using DungeonInn.Application.GameLoop;
using System;
using Cysharp.Threading.Tasks;
using VContainer;

namespace DungeonInn.Application.GameLoop
{
    public sealed class PauseGameTimeUseCase
    {
        readonly IGameClock gameClock;

        [Inject]
        public PauseGameTimeUseCase(IGameClock gameClock)
        {
            this.gameClock = gameClock ?? throw new ArgumentNullException(nameof(gameClock));
        }

        public UniTask ExecuteAsync()
        {
            gameClock.Pause();
            return UniTask.CompletedTask;
        }
    }
}
