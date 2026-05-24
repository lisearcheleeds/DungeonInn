using System;
using R3;
using VContainer;
using DungeonInn.Application.World;
using DungeonInn.View.Scene.Bridge;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD
{
    public sealed class ActorDetailPopupPresenter : IDisposable
    {
        readonly IActorSelectionReader actorSelectionReader;
        readonly GetActorDetailQuery actorDetailQuery;
        readonly IActorScreenPositionProvider screenPositionProvider;
        readonly GameHUDAddressableViewFactory viewFactory;
        readonly GameHUDModuleScene gameHUDModuleScene;
        ActorDetailPopup popup;
        DisposableBag bag;

        [Inject]
        public ActorDetailPopupPresenter(
            IActorSelectionReader actorSelectionReader,
            GetActorDetailQuery actorDetailQuery,
            IActorScreenPositionProvider screenPositionProvider,
            GameHUDAddressableViewFactory viewFactory,
            GameHUDModuleScene gameHUDModuleScene)
        {
            this.actorSelectionReader = actorSelectionReader ?? throw new ArgumentNullException(nameof(actorSelectionReader));
            this.actorDetailQuery = actorDetailQuery ?? throw new ArgumentNullException(nameof(actorDetailQuery));
            this.screenPositionProvider =
                screenPositionProvider ?? throw new ArgumentNullException(nameof(screenPositionProvider));
            this.viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
            this.gameHUDModuleScene = gameHUDModuleScene ?? throw new ArgumentNullException(nameof(gameHUDModuleScene));
        }

        public void Initialize()
        {
            EnsurePopup();
            if (popup != null)
            {
                popup.gameObject.SetActive(false);
            }

            actorSelectionReader.SelectedActorId
                .Subscribe(OnSelectedActorChanged)
                .AddTo(ref bag);
        }

        public void UpdatePopup()
        {
            var selectedId = actorSelectionReader.SelectedActorId.CurrentValue;
            if (!selectedId.HasValue)
            {
                return;
            }

            EnsurePopup();
            if (popup == null)
            {
                return;
            }

            var dto = actorDetailQuery.Query(selectedId.Value);
            if (!dto.HasValue)
            {
                return;
            }

            if (!screenPositionProvider.TryGetScreenPosition(dto.Value.Position, out var screenPosition))
            {
                return;
            }

            popup.SetPosition(screenPosition);
            popup.SetContent(dto.Value);
        }

        public void Dispose()
        {
            bag.Dispose();
        }

        void OnSelectedActorChanged(Guid? actorId)
        {
            EnsurePopup();
            if (popup != null)
            {
                popup.gameObject.SetActive(actorId.HasValue);
            }
        }

        void EnsurePopup()
        {
            if (popup != null)
            {
                return;
            }

            if (gameHUDModuleScene.HUDCanvas == null)
            {
                return;
            }

            popup = viewFactory.CreateActorDetailPopup(gameHUDModuleScene.HUDCanvas.transform);
        }
    }
}
