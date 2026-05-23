using System;
using DungeonInn.Application.World;
using R3;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class ActorDetailPopupPresenter : IDisposable
    {
        readonly ActorSelectionService actorSelectionService;
        readonly GetActorDetailQuery actorDetailQuery;
        readonly WorldCameraController worldCameraController;
        readonly LayerPositionViewMapper positionMapper;
        readonly MapLayerViewRegistry layerViewRegistry;
        readonly WorldAddressableViewFactory viewFactory;
        readonly WorldHudCanvasProvider hudCanvasProvider;
        ActorDetailPopup popup;
        DisposableBag bag;

        [Inject]
        public ActorDetailPopupPresenter(
            ActorSelectionService actorSelectionService,
            GetActorDetailQuery actorDetailQuery,
            WorldCameraController worldCameraController,
            LayerPositionViewMapper positionMapper,
            MapLayerViewRegistry layerViewRegistry,
            WorldAddressableViewFactory viewFactory,
            WorldHudCanvasProvider hudCanvasProvider)
        {
            this.actorSelectionService = actorSelectionService ?? throw new ArgumentNullException(nameof(actorSelectionService));
            this.actorDetailQuery = actorDetailQuery ?? throw new ArgumentNullException(nameof(actorDetailQuery));
            this.worldCameraController = worldCameraController ?? throw new ArgumentNullException(nameof(worldCameraController));
            this.positionMapper = positionMapper ?? throw new ArgumentNullException(nameof(positionMapper));
            this.layerViewRegistry = layerViewRegistry ?? throw new ArgumentNullException(nameof(layerViewRegistry));
            this.viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
            this.hudCanvasProvider = hudCanvasProvider ?? throw new ArgumentNullException(nameof(hudCanvasProvider));
        }

        public void Initialize()
        {
            EnsurePopup();
            if (popup != null)
            {
                popup.gameObject.SetActive(false);
            }

            actorSelectionService.SelectedActorId
                .Subscribe(OnSelectedActorChanged)
                .AddTo(ref bag);
        }

        public void UpdatePopupPosition()
        {
            var selectedId = actorSelectionService.SelectedActorId.Value;
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

            var localPosition = positionMapper.ToActorLayerLocalPosition(dto.Value.Position);
            var actorRoot = layerViewRegistry.GetOrCreateActorRoot(dto.Value.Position.LayerId);
            var worldPosition = actorRoot.TransformPoint(localPosition);
            var screenPosition = worldCameraController.WorldToScreenPoint(worldPosition);
            popup.SetPosition(new Vector2(screenPosition.x, screenPosition.y));
        }

        public void UpdatePopupContent()
        {
            var selectedId = actorSelectionService.SelectedActorId.Value;
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

            if (hudCanvasProvider.HUDCanvas == null)
            {
                return;
            }

            popup = viewFactory.CreateActorDetailPopup(hudCanvasProvider.HUDCanvas.transform);
        }
    }
}
