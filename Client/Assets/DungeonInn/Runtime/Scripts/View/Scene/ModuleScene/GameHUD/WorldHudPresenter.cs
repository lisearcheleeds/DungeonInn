using System;
using UnityEngine;
using R3;
using VContainer;
using DungeonInn.Application.Economy;
using DungeonInn.Application.Event;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Common;
using DungeonInn.View.Scene.MainScene.World;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD
{
    public sealed class WorldHudPresenter : IDisposable
    {
        const float AlertDisplaySeconds = 3f;
        const int MinutesPerDay = 1440;

        readonly GetGameTimeStateUseCase getGameTimeStateUseCase;
        readonly GetInnEconomyStatusUseCase getInnEconomyStatusUseCase;
        readonly ToggleGamePauseUseCase toggleGamePauseUseCase;
        readonly SetGameTimeScaleUseCase setGameTimeScaleUseCase;
        readonly GameHUDAddressableViewFactory viewFactory;
        readonly GameHUDModuleScene gameHUDModuleScene;
        readonly IEventSubscriber eventSubscriber;

        WorldHudView hudView;
        DisposableBag bag;
        float alertRemainingSeconds;

        [Inject]
        public WorldHudPresenter(
            GetGameTimeStateUseCase getGameTimeStateUseCase,
            GetInnEconomyStatusUseCase getInnEconomyStatusUseCase,
            ToggleGamePauseUseCase toggleGamePauseUseCase,
            SetGameTimeScaleUseCase setGameTimeScaleUseCase,
            GameHUDAddressableViewFactory viewFactory,
            GameHUDModuleScene gameHUDModuleScene,
            IEventSubscriber eventSubscriber)
        {
            this.getGameTimeStateUseCase =
                getGameTimeStateUseCase ?? throw new ArgumentNullException(nameof(getGameTimeStateUseCase));
            this.getInnEconomyStatusUseCase =
                getInnEconomyStatusUseCase ?? throw new ArgumentNullException(nameof(getInnEconomyStatusUseCase));
            this.toggleGamePauseUseCase =
                toggleGamePauseUseCase ?? throw new ArgumentNullException(nameof(toggleGamePauseUseCase));
            this.setGameTimeScaleUseCase =
                setGameTimeScaleUseCase ?? throw new ArgumentNullException(nameof(setGameTimeScaleUseCase));
            this.viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
            this.gameHUDModuleScene = gameHUDModuleScene ?? throw new ArgumentNullException(nameof(gameHUDModuleScene));
            this.eventSubscriber = eventSubscriber ?? throw new ArgumentNullException(nameof(eventSubscriber));
        }

        public void Initialize()
        {
            EnsureHudView();
            if (hudView == null)
            {
                return;
            }

            hudView.AddPauseListener(OnPauseClicked);
            hudView.AddSpeedNormalListener(OnSpeedNormalClicked);
            hudView.AddSpeedFastListener(OnSpeedFastClicked);
            hudView.HideAlert();

            eventSubscriber.OnEvent<IGameEvent>()
                .Subscribe(OnGameEvent)
                .AddTo(ref bag);

            UpdateHud();
        }

        public void UpdateHud()
        {
            EnsureHudView();
            if (hudView == null)
            {
                return;
            }

            var timeState = getGameTimeStateUseCase.Execute();
            hudView.SetDayTime(FormatDayTime(timeState));
            hudView.SetPauseButtonLabel(timeState.IsPaused ? "Resume" : "Pause");

            if (getInnEconomyStatusUseCase.CanExecute)
            {
                var economyStatus = getInnEconomyStatusUseCase.Execute();
                hudView.SetGold($"Gold: {economyStatus.Current.GuildGold:N0}");
            }

            UpdateAlert();
        }

        public void Dispose()
        {
            bag.Dispose();
        }

        static string FormatDayTime(GameTimeState timeState)
        {
            var gameTotalMinutes =
                timeState.CurrentTickOfDay * MinutesPerDay / GameConstants.GameScheduleTicksPerDay;
            var hours = gameTotalMinutes / 60;
            var minutes = gameTotalMinutes % 60;
            return $"Day {timeState.CurrentDay + 1}  {hours:D2}:{minutes:D2}";
        }

        void OnPauseClicked()
        {
            var timeState = toggleGamePauseUseCase.Execute();
            hudView?.SetPauseButtonLabel(timeState.IsPaused ? "Resume" : "Pause");
        }

        void OnSpeedNormalClicked()
        {
            setGameTimeScaleUseCase.Execute(1f);
        }

        void OnSpeedFastClicked()
        {
            setGameTimeScaleUseCase.Execute(2f);
        }

        void OnGameEvent(IGameEvent gameEvent)
        {
            EnsureHudView();
            if (hudView == null)
            {
                return;
            }

            var text = GameEventAlertFormatter.Format(gameEvent);
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            hudView.ShowAlert(text);
            alertRemainingSeconds = AlertDisplaySeconds;
        }

        void UpdateAlert()
        {
            if (alertRemainingSeconds <= 0f)
            {
                return;
            }

            alertRemainingSeconds -= Time.unscaledDeltaTime;
            if (alertRemainingSeconds <= 0f)
            {
                hudView.HideAlert();
            }
        }

        void EnsureHudView()
        {
            if (hudView != null || gameHUDModuleScene.HUDCanvas == null)
            {
                return;
            }

            hudView = viewFactory.CreateWorldHudView(gameHUDModuleScene.HUDCanvas.transform);
        }
    }
}
