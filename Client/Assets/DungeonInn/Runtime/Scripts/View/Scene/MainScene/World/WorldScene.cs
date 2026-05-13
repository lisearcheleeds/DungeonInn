using System.Threading;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.GameLoop;
using DungeonInn.Input;
using DungeonInn.Input.Layer;
using DungeonInn.LighthouseGenerated;
using DungeonInn.View.Base;
using LighthouseExtends.InputLayer;
using Lighthouse.Scene;
using Lighthouse.Scene.SceneCamera;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldScene : ProductMainSceneBase<WorldScene.WorldTransitionData>
    {
        [SerializeField] Camera worldCamera;

        IWorldPresenter worldPresenter;
        IGameWorldStateReader gameWorldState;
        ToggleGamePauseUseCase toggleGamePauseUseCase;
        GetInnEconomyStatusUseCase getInnEconomyStatusUseCase;
        WorldCameraController worldCameraController;
        WorldSceneCamera worldSceneCamera;
        bool isFallbackWorldCamera;

        public override MainSceneId MainSceneId => DungeonInnMainSceneId.World;

        public sealed class WorldTransitionData : ProductTransitionDataBase
        {
            public override MainSceneId MainSceneId => DungeonInnMainSceneId.World;
        }

        [Inject]
        public void Construct(
            IWorldPresenter worldPresenter,
            IGameWorldStateReader gameWorldState,
            ToggleGamePauseUseCase toggleGamePauseUseCase,
            GetInnEconomyStatusUseCase getInnEconomyStatusUseCase,
            WorldCameraController worldCameraController)
        {
            this.worldPresenter = worldPresenter;
            this.gameWorldState = gameWorldState;
            this.toggleGamePauseUseCase = toggleGamePauseUseCase;
            this.getInnEconomyStatusUseCase = getInnEconomyStatusUseCase;
            this.worldCameraController = worldCameraController;
        }

        public override ISceneCamera[] GetSceneCameraList()
        {
            EnsureWorldSceneCamera();
            return new ISceneCamera[] { worldSceneCamera };
        }

        protected override IInputLayer CreateInputLayer(InputActions inputActions)
        {
            return new WorldSceneInputLayer(
                inputActions,
                gameWorldState,
                toggleGamePauseUseCase,
                getInnEconomyStatusUseCase);
        }

        protected override InputActionMap GetInputLayerActionMap(InputActions inputActions)
        {
            return inputActions.Scene;
        }

        protected override UniTask OnSetup()
        {
            EnsureWorldSceneCamera();
            worldCameraController.BindCamera(
                worldSceneCamera.GetCamera(),
                applyInitialState: isFallbackWorldCamera);
            worldPresenter.Setup();
            return UniTask.CompletedTask;
        }

        protected override UniTask OnEnter(WorldTransitionData transitionData, ISceneTransitionContext context, CancellationToken cancelToken)
        {
            worldPresenter.OnEnter();
            return UniTask.CompletedTask;
        }

        void EnsureWorldSceneCamera()
        {
            if (worldSceneCamera != null)
            {
                return;
            }

            if (worldCamera != null)
            {
                EnsureUniversalCameraData(worldCamera.gameObject);
                worldSceneCamera = new WorldSceneCamera(worldCamera);
                return;
            }

            var cameraObject = new GameObject("WorldCamera");
            cameraObject.layer = WorldRenderingLayer.Layer;
            cameraObject.transform.SetParent(transform, false);
            worldCamera = cameraObject.AddComponent<Camera>();
            worldCamera.cullingMask = WorldRenderingLayer.Mask;
            EnsureUniversalCameraData(cameraObject);
            worldSceneCamera = new WorldSceneCamera(worldCamera);
            isFallbackWorldCamera = true;
        }

        static void EnsureUniversalCameraData(GameObject cameraObject)
        {
            if (cameraObject.GetComponent<UniversalAdditionalCameraData>() == null)
            {
                cameraObject.AddComponent<UniversalAdditionalCameraData>();
            }
        }
    }
}
