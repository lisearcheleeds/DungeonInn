using System;
using Cysharp.Threading.Tasks;
using VContainer;

namespace DungeonInn.Application.GameLoop
{
    public sealed class ResumeGameTimeUseCase
    {
        readonly IGameClock gameClock;

        [Inject]
        public ResumeGameTimeUseCase(IGameClock gameClock)
        {
            this.gameClock = gameClock ?? throw new ArgumentNullException(nameof(gameClock));
        }

        public UniTask ExecuteAsync()
        {
            gameClock.Resume();
            return UniTask.CompletedTask;
        }
    }
}
