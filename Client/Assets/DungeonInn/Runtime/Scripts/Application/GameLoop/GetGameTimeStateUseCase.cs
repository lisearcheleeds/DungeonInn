using System;
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

        public GameTimeState Execute()
        {
            return new GameTimeState(
                gameClock.TotalScheduleTick,
                gameClock.ElapsedRealTimeSeconds,
                gameClock.ElapsedGameTimeSeconds,
                gameClock.TimeScale,
                gameClock.IsPaused);
        }
    }
}
