using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Economy;
using DungeonInn.View.Scene.ModuleScene.GameHUD;
using LighthouseExtends.ScreenStack;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD.ScreenStack
{
    public sealed class GameHudWindowOpenService : IGameHudWindowOpenService
    {
        readonly IScreenStackModule screenStackModule;
        readonly GameHudScreenStackViewDataFactory viewDataFactory;
        readonly IGuildManagementScreenService guildManagementScreenService;
        readonly IMarketScreenService marketScreenService;

        [Inject]
        public GameHudWindowOpenService(
            IScreenStackModule screenStackModule,
            GameHudScreenStackViewDataFactory viewDataFactory,
            IGuildManagementScreenService guildManagementScreenService,
            IMarketScreenService marketScreenService)
        {
            this.screenStackModule = screenStackModule ?? throw new ArgumentNullException(nameof(screenStackModule));
            this.viewDataFactory = viewDataFactory ?? throw new ArgumentNullException(nameof(viewDataFactory));
            this.guildManagementScreenService =
                guildManagementScreenService ?? throw new ArgumentNullException(nameof(guildManagementScreenService));
            this.marketScreenService = marketScreenService ?? throw new ArgumentNullException(nameof(marketScreenService));
        }

        public void OpenDungeonInfo()
        {
            Debug.Log("[GameHUD.ScreenStack] Open requested: DungeonInfoWindow");
            screenStackModule.Open(new DungeonInfoWindowData(viewDataFactory.CreateDungeonInfo())).Forget();
        }

        public void OpenGuildManagement()
        {
            Debug.Log("[GameHUD.ScreenStack] Open requested: GuildManagementWindow");
            screenStackModule.Open(new GuildManagementWindowData(
                viewDataFactory.CreateGuildManagement(),
                viewDataFactory.CreateGuildManagement,
                facilityId => guildManagementScreenService.UpgradeFacility(facilityId))).Forget();
        }

        public void OpenMarket()
        {
            Debug.Log("[GameHUD.ScreenStack] Open requested: MarketWindow");
            screenStackModule.Open(new MarketWindowData(
                viewDataFactory.CreateMarket(),
                viewDataFactory.CreateMarket,
                offerId => marketScreenService.FulfillOffer(offerId))).Forget();
        }
    }
}
