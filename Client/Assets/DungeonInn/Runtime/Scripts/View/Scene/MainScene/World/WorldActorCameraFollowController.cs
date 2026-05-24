using System;
using DungeonInn.Application.World;
using DungeonInn.View.Scene.Bridge;
using R3;
using VContainer;
using VContainer.Unity;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldActorCameraFollowController : IInitializable, IDisposable
    {
        readonly ActorSelectionService actorSelectionService;
        readonly WorldCameraController worldCameraController;
        readonly IWorldCameraSettingsRepository cameraSettingsRepository;
        readonly IGameWorldStateReader worldState;
        readonly LayerPositionViewMapper positionMapper;
        readonly MapLayerViewRegistry layerViewRegistry;
        DisposableBag bag;

        [Inject]
        public WorldActorCameraFollowController(
            ActorSelectionService actorSelectionService,
            WorldCameraController worldCameraController,
            IWorldCameraSettingsRepository cameraSettingsRepository,
            IGameWorldStateReader worldState,
            LayerPositionViewMapper positionMapper,
            MapLayerViewRegistry layerViewRegistry)
        {
            this.actorSelectionService = actorSelectionService ?? throw new ArgumentNullException(nameof(actorSelectionService));
            this.worldCameraController = worldCameraController ?? throw new ArgumentNullException(nameof(worldCameraController));
            this.cameraSettingsRepository =
                cameraSettingsRepository ?? throw new ArgumentNullException(nameof(cameraSettingsRepository));
            this.worldState = worldState ?? throw new ArgumentNullException(nameof(worldState));
            this.positionMapper = positionMapper ?? throw new ArgumentNullException(nameof(positionMapper));
            this.layerViewRegistry = layerViewRegistry ?? throw new ArgumentNullException(nameof(layerViewRegistry));
        }

        public void Initialize()
        {
            actorSelectionService.SelectedActorId
                .Subscribe(OnSelectedActorChanged)
                .AddTo(ref bag);
        }

        public void UpdateFollowPosition()
        {
            var selectedId = actorSelectionService.SelectedActorId.Value;
            if (!selectedId.HasValue)
            {
                return;
            }

            var actor = worldState.FindActor(selectedId.Value);
            if (actor == null)
            {
                actorSelectionService.Deselect();
                return;
            }

            var localPosition = positionMapper.ToActorLayerLocalPosition(actor.Position);
            var actorRoot = layerViewRegistry.GetOrCreateActorRoot(actor.Position.LayerId);
            layerViewRegistry.SelectLayer(actor.Position.LayerId);
            var worldPosition = actorRoot.TransformPoint(localPosition);
            worldCameraController.UpdateFollowPosition(worldPosition);
        }

        public void Dispose()
        {
            bag.Dispose();
        }

        void OnSelectedActorChanged(Guid? actorId)
        {
            if (actorId.HasValue)
            {
                worldCameraController.BeginFollow(cameraSettingsRepository.Get().ActorSelectionZoomRatio);
            }
            else
            {
                worldCameraController.EndFollow();
            }
        }
    }
}
