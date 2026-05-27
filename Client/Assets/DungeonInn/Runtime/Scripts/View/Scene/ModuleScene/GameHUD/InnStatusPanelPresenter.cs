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

        readonly IInnStatusPanelScreenService innStatusPanelScreenService;
        readonly GameHUDAddressableViewFactory viewFactory;
        readonly GameHUDModuleScene gameHUDModuleScene;
        readonly List<InnGuestSummary> guestBuffer = new();

        InnStatusPanelView panelView;
        float elapsedSinceUpdate;

        [Inject]
        public InnStatusPanelPresenter(
            IInnStatusPanelScreenService innStatusPanelScreenService,
            GameHUDAddressableViewFactory viewFactory,
            GameHUDModuleScene gameHUDModuleScene)
        {
            this.innStatusPanelScreenService =
                innStatusPanelScreenService ?? throw new ArgumentNullException(nameof(innStatusPanelScreenService));
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

            if (innStatusPanelScreenService.CanGetGuestList)
            {
                innStatusPanelScreenService.FillGuests(guestBuffer);
                panelView.SetGuests(guestBuffer);
            }

            if (innStatusPanelScreenService.CanGetEconomyStatus)
            {
                var economyStatus = innStatusPanelScreenService.GetEconomyStatus();
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
