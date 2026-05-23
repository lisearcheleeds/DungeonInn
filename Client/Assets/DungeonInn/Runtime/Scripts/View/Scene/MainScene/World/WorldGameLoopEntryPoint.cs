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
        WorldActorStatusPresenter worldActorStatusPresenter;
        WorldActorCameraFollowController worldActorCameraFollowController;
        ActorDetailPopupPresenter actorDetailPopupPresenter;
        PlayerGameEventLogPresenter playerGameEventLogPresenter;
        WorldCameraController worldCameraController;
        MapLayerViewRegistry layerViewRegistry;
        VisualConfigLoader visualConfigLoader;
        WorldAddressableViewFactory viewFactory;

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
            WorldActorStatusPresenter worldActorStatusPresenter,
            WorldActorCameraFollowController worldActorCameraFollowController,
            WorldCameraController worldCameraController,
            MapLayerViewRegistry layerViewRegistry,
            VisualConfigLoader visualConfigLoader,
            WorldAddressableViewFactory viewFactory,
            ActorDetailPopupPresenter actorDetailPopupPresenter,
            PlayerGameEventLogPresenter playerGameEventLogPresenter)
        {
            this.worldSimulationOrchestrator = worldSimulationOrchestrator ?? throw new ArgumentNullException(nameof(worldSimulationOrchestrator));
            this.worldMapView = worldMapView ?? throw new ArgumentNullException(nameof(worldMapView));
            this.worldActorPresenter = worldActorPresenter ?? throw new ArgumentNullException(nameof(worldActorPresenter));
            this.worldProjectilePresenter =
                worldProjectilePresenter ?? throw new ArgumentNullException(nameof(worldProjectilePresenter));
            this.worldAreaEffectPresenter =
                worldAreaEffectPresenter ?? throw new ArgumentNullException(nameof(worldAreaEffectPresenter));
            this.worldActorStatusPresenter =
                worldActorStatusPresenter ?? throw new ArgumentNullException(nameof(worldActorStatusPresenter));
            this.worldActorCameraFollowController =
                worldActorCameraFollowController ?? throw new ArgumentNullException(nameof(worldActorCameraFollowController));
            this.worldCameraController = worldCameraController ?? throw new ArgumentNullException(nameof(worldCameraController));
            this.layerViewRegistry = layerViewRegistry ?? throw new ArgumentNullException(nameof(layerViewRegistry));
            this.visualConfigLoader = visualConfigLoader ?? throw new ArgumentNullException(nameof(visualConfigLoader));
            this.viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
            this.actorDetailPopupPresenter = actorDetailPopupPresenter ?? throw new ArgumentNullException(nameof(actorDetailPopupPresenter));
            this.playerGameEventLogPresenter = playerGameEventLogPresenter ?? throw new ArgumentNullException(nameof(playerGameEventLogPresenter));
        }

        void Start()
        {
            Debug.Log("[WorldGameLoop] EntryPoint started.");
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
            actorDetailPopupPresenter.UpdatePopup();
            worldMapView.UpdateVisuals();
            worldActorPresenter.UpdateVisuals();
            worldProjectilePresenter.UpdatePositions();
            worldAreaEffectPresenter.UpdatePositions();
            worldActorStatusPresenter.UpdatePositions();

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
                await visualConfigLoader.LoadAsync(cancellationToken);
                await viewFactory.LoadAsync(cancellationToken);
                actorDetailPopupPresenter?.Initialize();
                playerGameEventLogPresenter?.Initialize();
                var result = await worldSimulationOrchestrator.InitializeAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                isInitialized = true;
                Debug.Log(
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
