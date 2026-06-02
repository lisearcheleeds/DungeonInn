using System;
using DungeonInn.Application.World;
using DungeonInn.View.Scene.Bridge;
using R3;
using VContainer;

namespace DungeonInn.View.Scene.ModuleScene.GameUI
{
    public sealed class SelectedActorInspectorPresenter : IDisposable
    {
        readonly IActorSelectionReader actorSelectionReader;
        readonly GetSelectedActorInspectorQuery selectedActorInspectorQuery;
        readonly GameUIAddressableViewFactory viewFactory;
        readonly GameUIModuleScene gameUIModuleScene;
        SelectedActorInspectorView view;
        DisposableBag bag;

        [Inject]
        public SelectedActorInspectorPresenter(
            IActorSelectionReader actorSelectionReader,
            GetSelectedActorInspectorQuery selectedActorInspectorQuery,
            GameUIAddressableViewFactory viewFactory,
            GameUIModuleScene gameUIModuleScene)
        {
            this.actorSelectionReader = actorSelectionReader ?? throw new ArgumentNullException(nameof(actorSelectionReader));
            this.selectedActorInspectorQuery =
                selectedActorInspectorQuery ?? throw new ArgumentNullException(nameof(selectedActorInspectorQuery));
            this.viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
            this.gameUIModuleScene = gameUIModuleScene ?? throw new ArgumentNullException(nameof(gameUIModuleScene));
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

            if (gameUIModuleScene.UICanvas == null)
            {
                return;
            }

            view = viewFactory.CreateSelectedActorInspectorView(gameUIModuleScene.UICanvas.transform);
        }
    }
}

