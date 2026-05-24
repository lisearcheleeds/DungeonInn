using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using DungeonInn.Application.Economy;
using DungeonInn.Application.World;
using DungeonInn.View.Scene.MainScene.World;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD
{
    public sealed class InnStatusPanelPresenter : IDisposable
    {
        const float UpdateIntervalSeconds = 0.5f;

        readonly GetInnGuestListUseCase getInnGuestListUseCase;
        readonly GetInnEconomyStatusUseCase getInnEconomyStatusUseCase;
        readonly GameHUDAddressableViewFactory viewFactory;
        readonly GameHUDModuleScene gameHUDModuleScene;
        readonly List<InnGuestSummary> guestBuffer = new();

        InnStatusPanelView panelView;
        float elapsedSinceUpdate;

        [Inject]
        public InnStatusPanelPresenter(
            GetInnGuestListUseCase getInnGuestListUseCase,
            GetInnEconomyStatusUseCase getInnEconomyStatusUseCase,
            GameHUDAddressableViewFactory viewFactory,
            GameHUDModuleScene gameHUDModuleScene)
        {
            this.getInnGuestListUseCase =
                getInnGuestListUseCase ?? throw new ArgumentNullException(nameof(getInnGuestListUseCase));
            this.getInnEconomyStatusUseCase =
                getInnEconomyStatusUseCase ?? throw new ArgumentNullException(nameof(getInnEconomyStatusUseCase));
            this.viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
            this.gameHUDModuleScene = gameHUDModuleScene ?? throw new ArgumentNullException(nameof(gameHUDModuleScene));
        }

        public void Initialize()
        {
            EnsurePanelView();
            UpdatePanelCore();
        }

        public void UpdatePanel()
        {
            elapsedSinceUpdate += Time.unscaledDeltaTime;
            if (elapsedSinceUpdate < UpdateIntervalSeconds)
            {
                return;
            }

            elapsedSinceUpdate = 0f;
            UpdatePanelCore();
        }

        public void Dispose()
        {
            if (panelView == null)
            {
                return;
            }

            UnityEngine.Object.Destroy(panelView.gameObject);
            panelView = null;
        }

        void UpdatePanelCore()
        {
            EnsurePanelView();
            if (panelView == null)
            {
                return;
            }

            if (getInnGuestListUseCase.CanExecute)
            {
                getInnGuestListUseCase.Execute(guestBuffer);
                panelView.SetGuests(guestBuffer);
            }

            if (getInnEconomyStatusUseCase.CanExecute)
            {
                var economyStatus = getInnEconomyStatusUseCase.Execute();
                panelView.SetEconomySummary(economyStatus);
            }
        }

        void EnsurePanelView()
        {
            if (panelView != null || gameHUDModuleScene.HUDCanvas == null)
            {
                return;
            }

            panelView = viewFactory.CreateInnStatusPanelView(gameHUDModuleScene.HUDCanvas.transform);
        }
    }
}
