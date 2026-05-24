using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer;
using VContainer.Unity;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD
{
    public sealed class GameHUDEntryPoint : IAsyncStartable, ITickable
    {
        readonly GameHUDAddressableViewFactory viewFactory;
        readonly WorldActorStatusPresenter worldActorStatusPresenter;
        readonly ActorDetailPopupPresenter actorDetailPopupPresenter;
        readonly PlayerGameEventLogPresenter playerGameEventLogPresenter;
        readonly WorldHudPresenter worldHudPresenter;
        readonly InnStatusPanelPresenter innStatusPanelPresenter;
        readonly MinimapPresenter minimapPresenter;
        bool isInitialized;

        [Inject]
        public GameHUDEntryPoint(
            GameHUDAddressableViewFactory viewFactory,
            WorldActorStatusPresenter worldActorStatusPresenter,
            ActorDetailPopupPresenter actorDetailPopupPresenter,
            PlayerGameEventLogPresenter playerGameEventLogPresenter,
            WorldHudPresenter worldHudPresenter,
            InnStatusPanelPresenter innStatusPanelPresenter,
            MinimapPresenter minimapPresenter)
        {
            this.viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
            this.worldActorStatusPresenter =
                worldActorStatusPresenter ?? throw new ArgumentNullException(nameof(worldActorStatusPresenter));
            this.actorDetailPopupPresenter =
                actorDetailPopupPresenter ?? throw new ArgumentNullException(nameof(actorDetailPopupPresenter));
            this.playerGameEventLogPresenter =
                playerGameEventLogPresenter ?? throw new ArgumentNullException(nameof(playerGameEventLogPresenter));
            this.worldHudPresenter = worldHudPresenter ?? throw new ArgumentNullException(nameof(worldHudPresenter));
            this.innStatusPanelPresenter =
                innStatusPanelPresenter ?? throw new ArgumentNullException(nameof(innStatusPanelPresenter));
            this.minimapPresenter = minimapPresenter ?? throw new ArgumentNullException(nameof(minimapPresenter));
        }

        public async UniTask StartAsync(CancellationToken cancellation)
        {
            await viewFactory.LoadAsync(cancellation);
            actorDetailPopupPresenter.Initialize();
            playerGameEventLogPresenter.Initialize();
            worldHudPresenter.Initialize();
            innStatusPanelPresenter.Initialize();
            minimapPresenter.Initialize();
            isInitialized = true;
        }

        public void Tick()
        {
            if (!isInitialized)
            {
                return;
            }

            actorDetailPopupPresenter.UpdatePopup();
            worldActorStatusPresenter.UpdatePositions();
            worldHudPresenter.UpdateHud();
            innStatusPanelPresenter.UpdatePanel();
            minimapPresenter.UpdateMinimap();
        }
    }
}
