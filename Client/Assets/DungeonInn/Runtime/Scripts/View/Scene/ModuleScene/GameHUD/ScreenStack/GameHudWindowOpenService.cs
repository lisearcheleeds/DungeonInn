using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Economy;
using DungeonInn.Application.SaveLoad;
using DungeonInn.Core;
using DungeonInn.GameSession;
using DungeonInn.View.Scene.MainScene.Title;
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
        readonly GetSaveSlotSummariesUseCase getSaveSlotSummariesUseCase;
        readonly SaveGameUseCase saveGameUseCase;
        readonly ActiveSaveSlotService activeSaveSlotService;
        readonly GameSessionStartCoordinator gameSessionStartCoordinator;
        readonly IProductSceneManager sceneManager;

        [Inject]
        public GameHudWindowOpenService(
            IScreenStackModule screenStackModule,
            GameHudScreenStackViewDataFactory viewDataFactory,
            IGuildManagementScreenService guildManagementScreenService,
            IMarketScreenService marketScreenService,
            GetSaveSlotSummariesUseCase getSaveSlotSummariesUseCase,
            SaveGameUseCase saveGameUseCase,
            ActiveSaveSlotService activeSaveSlotService,
            GameSessionStartCoordinator gameSessionStartCoordinator,
            IProductSceneManager sceneManager)
        {
            this.screenStackModule = screenStackModule ?? throw new ArgumentNullException(nameof(screenStackModule));
            this.viewDataFactory = viewDataFactory ?? throw new ArgumentNullException(nameof(viewDataFactory));
            this.guildManagementScreenService =
                guildManagementScreenService ?? throw new ArgumentNullException(nameof(guildManagementScreenService));
            this.marketScreenService = marketScreenService ?? throw new ArgumentNullException(nameof(marketScreenService));
            this.getSaveSlotSummariesUseCase = getSaveSlotSummariesUseCase
                ?? throw new ArgumentNullException(nameof(getSaveSlotSummariesUseCase));
            this.saveGameUseCase = saveGameUseCase ?? throw new ArgumentNullException(nameof(saveGameUseCase));
            this.activeSaveSlotService = activeSaveSlotService
                ?? throw new ArgumentNullException(nameof(activeSaveSlotService));
            this.gameSessionStartCoordinator = gameSessionStartCoordinator
                ?? throw new ArgumentNullException(nameof(gameSessionStartCoordinator));
            this.sceneManager = sceneManager ?? throw new ArgumentNullException(nameof(sceneManager));
        }

        public void OpenSystemMenu()
        {
            Debug.Log("[GameHUD.ScreenStack] Open requested: SystemMenuWindow");
            screenStackModule.Open(new SystemMenuWindowData(
                OpenSaveSlotSelection,
                OpenLoadSlotSelection,
                ReturnToTitle)).Forget();
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

        void OpenSaveSlotSelection()
        {
            screenStackModule.Open(new SaveSlotSelectionWindowData(
                "Save Game",
                getSaveSlotSummariesUseCase.Execute(),
                activeSaveSlotService.ActiveSlotId,
                true,
                slotId => saveGameUseCase.Execute(slotId))).Forget();
        }

        void OpenLoadSlotSelection()
        {
            screenStackModule.Open(new SaveSlotSelectionWindowData(
                "Load Game",
                getSaveSlotSummariesUseCase.Execute(),
                activeSaveSlotService.ActiveSlotId,
                false,
                LoadFromSlot)).Forget();
        }

        void LoadFromSlot(int slotId)
        {
            UniTask.Void(async () =>
            {
                await gameSessionStartCoordinator.TryReloadGameAsync(slotId);
            });
        }

        void ReturnToTitle()
        {
            UniTask.Void(async () =>
            {
                await sceneManager.TransitionScene(new TitleScene.TitleTransitionData());
            });
        }
    }
}
