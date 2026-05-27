using System;
using DungeonInn.Application.Economy;
using VContainer;

namespace DungeonInn.Application.GameLoop
{
    public sealed class WorldHudScreenService : IWorldHudScreenService
    {
        readonly GetGameTimeStateUseCase getGameTimeStateUseCase;
        readonly GetInnEconomyStatusUseCase getInnEconomyStatusUseCase;
        readonly ToggleGamePauseUseCase toggleGamePauseUseCase;
        readonly SetGameTimeScaleUseCase setGameTimeScaleUseCase;

        [Inject]
        public WorldHudScreenService(
            GetGameTimeStateUseCase getGameTimeStateUseCase,
            GetInnEconomyStatusUseCase getInnEconomyStatusUseCase,
            ToggleGamePauseUseCase toggleGamePauseUseCase,
            SetGameTimeScaleUseCase setGameTimeScaleUseCase)
        {
            this.getGameTimeStateUseCase =
                getGameTimeStateUseCase ?? throw new ArgumentNullException(nameof(getGameTimeStateUseCase));
            this.getInnEconomyStatusUseCase =
                getInnEconomyStatusUseCase ?? throw new ArgumentNullException(nameof(getInnEconomyStatusUseCase));
            this.toggleGamePauseUseCase =
                toggleGamePauseUseCase ?? throw new ArgumentNullException(nameof(toggleGamePauseUseCase));
            this.setGameTimeScaleUseCase =
                setGameTimeScaleUseCase ?? throw new ArgumentNullException(nameof(setGameTimeScaleUseCase));
        }

        public bool CanGetInnEconomyStatus => getInnEconomyStatusUseCase.CanExecute;

        public GameTimeState GetTimeState()
        {
            return getGameTimeStateUseCase.Execute();
        }

        public InnEconomyStatus GetInnEconomyStatus()
        {
            return getInnEconomyStatusUseCase.Execute();
        }

        public GameTimeState TogglePause()
        {
            return toggleGamePauseUseCase.Execute();
        }

        public void SetTimeScale(float timeScale)
        {
            setGameTimeScaleUseCase.Execute(timeScale);
        }
    }
}
