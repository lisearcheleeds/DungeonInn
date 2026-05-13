using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.GameLoop;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldGameLoopEntryPoint : MonoBehaviour
    {
        IWorldSimulationOrchestrator worldSimulationOrchestrator;
        WorldMapView worldMapView;
        WorldActorPresenter worldActorPresenter;
        WorldCameraController worldCameraController;

        readonly CancellationTokenSource destroyCancellationTokenSource = new();

        bool isExecuting;
        bool isInitialized;

        [Inject]
        public void Construct(
            IWorldSimulationOrchestrator worldSimulationOrchestrator,
            WorldMapView worldMapView,
            WorldActorPresenter worldActorPresenter,
            WorldCameraController worldCameraController)
        {
            this.worldSimulationOrchestrator = worldSimulationOrchestrator ?? throw new ArgumentNullException(nameof(worldSimulationOrchestrator));
            this.worldMapView = worldMapView ?? throw new ArgumentNullException(nameof(worldMapView));
            this.worldActorPresenter = worldActorPresenter ?? throw new ArgumentNullException(nameof(worldActorPresenter));
            this.worldCameraController = worldCameraController ?? throw new ArgumentNullException(nameof(worldCameraController));
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
            worldMapView.UpdateVisuals();
            worldActorPresenter.UpdateVisuals();

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
                    new WorldFrameAdvanceRequest(Time.unscaledDeltaTime, cancellationToken));
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
