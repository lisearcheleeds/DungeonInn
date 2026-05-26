using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.World;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldGameLoopEntryPoint : MonoBehaviour
    {
        IWorldSimulationOrchestrator worldSimulationOrchestrator;
        WorldMapView worldMapView;
        WorldActorPresenter worldActorPresenter;
        WorldProjectilePresenter worldProjectilePresenter;
        WorldAreaEffectPresenter worldAreaEffectPresenter;
        WorldActorCameraFollowController worldActorCameraFollowController;
        WorldCameraController worldCameraController;
        MapLayerViewRegistry layerViewRegistry;
        VisualConfigLoader visualConfigLoader;
        WorldAddressableViewFactory viewFactory;
        IWorldMapViewSettingsRepository worldMapViewSettingsRepository;
        ILayerPositionViewSettingsRepository layerPositionViewSettingsRepository;
        IWorldCameraSettingsRepository worldCameraSettingsRepository;

        readonly CancellationTokenSource destroyCancellationTokenSource = new();

        bool isExecuting;
        bool isInitialized;

        [Inject]
        public void Construct(
            IWorldSimulationOrchestrator worldSimulationOrchestrator,
            WorldMapView worldMapView,
            WorldActorPresenter worldActorPresenter,
            WorldProjectilePresenter worldProjectilePresenter,
            WorldAreaEffectPresenter worldAreaEffectPresenter,
            WorldActorCameraFollowController worldActorCameraFollowController,
            WorldCameraController worldCameraController,
            MapLayerViewRegistry layerViewRegistry,
            VisualConfigLoader visualConfigLoader,
            WorldAddressableViewFactory viewFactory,
            IWorldMapViewSettingsRepository worldMapViewSettingsRepository,
            ILayerPositionViewSettingsRepository layerPositionViewSettingsRepository,
            IWorldCameraSettingsRepository worldCameraSettingsRepository)
        {
            this.worldSimulationOrchestrator = worldSimulationOrchestrator ?? throw new ArgumentNullException(nameof(worldSimulationOrchestrator));
            this.worldMapView = worldMapView ?? throw new ArgumentNullException(nameof(worldMapView));
            this.worldActorPresenter = worldActorPresenter ?? throw new ArgumentNullException(nameof(worldActorPresenter));
            this.worldProjectilePresenter =
                worldProjectilePresenter ?? throw new ArgumentNullException(nameof(worldProjectilePresenter));
            this.worldAreaEffectPresenter =
                worldAreaEffectPresenter ?? throw new ArgumentNullException(nameof(worldAreaEffectPresenter));
            this.worldActorCameraFollowController =
                worldActorCameraFollowController ?? throw new ArgumentNullException(nameof(worldActorCameraFollowController));
            this.worldCameraController = worldCameraController ?? throw new ArgumentNullException(nameof(worldCameraController));
            this.layerViewRegistry = layerViewRegistry ?? throw new ArgumentNullException(nameof(layerViewRegistry));
            this.visualConfigLoader = visualConfigLoader ?? throw new ArgumentNullException(nameof(visualConfigLoader));
            this.viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
            this.worldMapViewSettingsRepository =
                worldMapViewSettingsRepository ?? throw new ArgumentNullException(nameof(worldMapViewSettingsRepository));
            this.layerPositionViewSettingsRepository =
                layerPositionViewSettingsRepository ?? throw new ArgumentNullException(nameof(layerPositionViewSettingsRepository));
            this.worldCameraSettingsRepository =
                worldCameraSettingsRepository ?? throw new ArgumentNullException(nameof(worldCameraSettingsRepository));
        }

        void Start()
        {
            UnityEngine.Debug.Log("[WorldGameLoop] EntryPoint started.");
            InitializeAsync(destroyCancellationTokenSource.Token).Forget();
        }

        void OnDestroy()
        {
            destroyCancellationTokenSource.Cancel();
            destroyCancellationTokenSource.Dispose();
        }

        void Update()
        {
            if (!isInitialized || worldSimulationOrchestrator == null)
            {
                return;
            }

            worldCameraController.UpdateCamera(Time.unscaledDeltaTime);
            worldActorCameraFollowController.UpdateFollowPosition();
            worldMapView.UpdateVisuals();
            worldActorPresenter.UpdateVisuals();
            worldProjectilePresenter.UpdatePositions();
            worldAreaEffectPresenter.UpdatePositions();

            if (isExecuting)
            {
                return;
            }

            TickAsync(destroyCancellationTokenSource.Token).Forget();
        }

        async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            try
            {
                await worldMapViewSettingsRepository.LoadAsync(cancellationToken);
                await layerPositionViewSettingsRepository.LoadAsync(cancellationToken);
                await worldCameraSettingsRepository.LoadAsync(cancellationToken);
                await visualConfigLoader.LoadAsync(cancellationToken);
                await viewFactory.LoadAsync(cancellationToken);
                var result = await worldSimulationOrchestrator.InitializeAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                isInitialized = true;
                UnityEngine.Debug.Log(
                    $"[World] GameWorldState initialized. " +
                    $"Facilities={result.FacilityCount} " +
                    $"DungeonFloors={result.DungeonFloorCount} " +
                    $"Actors={result.ActorCount}");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
        }

        async UniTask TickAsync(CancellationToken cancellationToken)
        {
            isExecuting = true;
            try
            {
                await worldSimulationOrchestrator.AdvanceFrameAsync(
                    new WorldFrameAdvanceRequest(
                        Time.unscaledDeltaTime,
                        cancellationToken,
                        layerViewRegistry.ActiveLayerId));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            finally
            {
                isExecuting = false;
            }
        }
    }
}
