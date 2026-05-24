using System;
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

        public void Execute(float timeScale)
        {
            gameClock.SetTimeScale(timeScale);
        }
    }
}
