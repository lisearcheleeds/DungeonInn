using System;
using DungeonInn.Application.World;
using DungeonInn.View.Scene.Bridge;
using R3;
using VContainer;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD
{
    public sealed class SelectedActorInspectorPresenter : IDisposable
    {
        readonly IActorSelectionReader actorSelectionReader;
        readonly GetSelectedActorInspectorQuery selectedActorInspectorQuery;
        readonly GameHUDAddressableViewFactory viewFactory;
        readonly GameHUDModuleScene gameHUDModuleScene;
        SelectedActorInspectorView view;
        DisposableBag bag;

        [Inject]
        public SelectedActorInspectorPresenter(
            IActorSelectionReader actorSelectionReader,
            GetSelectedActorInspectorQuery selectedActorInspectorQuery,
            GameHUDAddressableViewFactory viewFactory,
            GameHUDModuleScene gameHUDModuleScene)
        {
            this.actorSelectionReader = actorSelectionReader ?? throw new ArgumentNullException(nameof(actorSelectionReader));
            this.selectedActorInspectorQuery =
                selectedActorInspectorQuery ?? throw new ArgumentNullException(nameof(selectedActorInspectorQuery));
            this.viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
            this.gameHUDModuleScene = gameHUDModuleScene ?? throw new ArgumentNullException(nameof(gameHUDModuleScene));
        }

        public void Initialize()
        {
            EnsureView();
            view?.SetVisible(false);
            actorSelectionReader.SelectedActorId
                .Subscribe(OnSelectedActorChanged)
                .AddTo(ref bag);
        }

        public void UpdateInspector()
        {
            var selectedId = actorSelectionReader.SelectedActorId.CurrentValue;
            if (!selectedId.HasValue)
            {
                return;
            }

            EnsureView();
            if (view == null)
            {
                return;
            }

            var viewData = selectedActorInspectorQuery.Query(selectedId.Value);
            if (viewData == null)
            {
                view.SetVisible(false);
                view.Clear();
                return;
            }

            view.SetVisible(true);
            view.SetContent(viewData);
        }

        public void Dispose()
        {
            bag.Dispose();
        }

        void OnSelectedActorChanged(Guid? actorId)
        {
            EnsureView();
            if (view == null)
            {
                return;
            }

            view.SetVisible(actorId.HasValue);
            if (!actorId.HasValue)
            {
                view.Clear();
            }
        }

        void EnsureView()
        {
            if (view != null)
            {
                return;
            }

            if (gameHUDModuleScene.HUDCanvas == null)
            {
                return;
            }

            view = viewFactory.CreateSelectedActorInspectorView(gameHUDModuleScene.HUDCanvas.transform);
        }
    }
}
