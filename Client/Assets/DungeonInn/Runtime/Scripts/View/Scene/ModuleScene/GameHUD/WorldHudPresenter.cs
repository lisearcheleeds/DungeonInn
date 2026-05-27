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
        const int MinutesPerDay = 1440;

        readonly IWorldHudScreenService worldHudScreenService;
        readonly GameHUDAddressableViewFactory viewFactory;
        readonly GameHUDModuleScene gameHUDModuleScene;
        readonly IEventSubscriber eventSubscriber;
        readonly IGameHudWindowOpenService windowOpenService;

        WorldHudView hudView;
        DisposableBag bag;

        [Inject]
        public WorldHudPresenter(
            IWorldHudScreenService worldHudScreenService,
            GameHUDAddressableViewFactory viewFactory,
            GameHUDModuleScene gameHUDModuleScene,
            IEventSubscriber eventSubscriber,
            IGameHudWindowOpenService windowOpenService)
        {
            this.worldHudScreenService =
                worldHudScreenService ?? throw new ArgumentNullException(nameof(worldHudScreenService));
            this.viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
            this.gameHUDModuleScene = gameHUDModuleScene ?? throw new ArgumentNullException(nameof(gameHUDModuleScene));
            this.eventSubscriber = eventSubscriber ?? throw new ArgumentNullException(nameof(eventSubscriber));
            this.windowOpenService = windowOpenService ?? throw new ArgumentNullException(nameof(windowOpenService));
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
            hudView.AddDungeonInfoListener(OnDungeonInfoClicked);
            hudView.AddGuildManagementListener(OnGuildManagementClicked);
            hudView.AddMarketListener(OnMarketClicked);

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

            var timeState = worldHudScreenService.GetTimeState();
            hudView.SetDayTime(FormatDayTime(timeState));
            hudView.SetPauseButtonLabel(timeState.IsPaused ? "Resume" : "Pause");

            if (worldHudScreenService.CanGetInnEconomyStatus)
            {
                var economyStatus = worldHudScreenService.GetInnEconomyStatus();
                hudView.SetGold($"Gold: {economyStatus.Current.GuildGold:N0}");
            }
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
            var timeState = worldHudScreenService.TogglePause();
            hudView?.SetPauseButtonLabel(timeState.IsPaused ? "Resume" : "Pause");
        }

        void OnSpeedNormalClicked()
        {
            worldHudScreenService.SetTimeScale(1f);
        }

        void OnSpeedFastClicked()
        {
            worldHudScreenService.SetTimeScale(2f);
        }

        void OnDungeonInfoClicked()
        {
            windowOpenService.OpenDungeonInfo();
        }

        void OnGuildManagementClicked()
        {
            windowOpenService.OpenGuildManagement();
        }

        void OnMarketClicked()
        {
            windowOpenService.OpenMarket();
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
