using System.Threading;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.GameLoop;
using DungeonInn.Input;
using DungeonInn.Input.Layer;
using DungeonInn.LighthouseGenerated;
using DungeonInn.View.Base;
using LighthouseExtends.InputLayer;
using Lighthouse.Scene;
using UnityEngine.InputSystem;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldScene : ProductMainSceneBase<WorldScene.WorldTransitionData>
    {
        IWorldPresenter worldPresenter;
        IGameWorldState gameWorldState;
        ToggleGamePauseUseCase toggleGamePauseUseCase;
        GetInnEconomyStatusUseCase getInnEconomyStatusUseCase;

        public override MainSceneId MainSceneId => DungeonInnMainSceneId.World;

        public sealed class WorldTransitionData : ProductTransitionDataBase
        {
            public override MainSceneId MainSceneId => DungeonInnMainSceneId.World;
        }

        [Inject]
        public void Construct(
            IWorldPresenter worldPresenter,
            IGameWorldState gameWorldState,
            ToggleGamePauseUseCase toggleGamePauseUseCase,
            GetInnEconomyStatusUseCase getInnEconomyStatusUseCase)
        {
            this.worldPresenter = worldPresenter;
            this.gameWorldState = gameWorldState;
            this.toggleGamePauseUseCase = toggleGamePauseUseCase;
            this.getInnEconomyStatusUseCase = getInnEconomyStatusUseCase;
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
            worldPresenter.Setup();
            return UniTask.CompletedTask;
        }

        protected override UniTask OnEnter(WorldTransitionData transitionData, ISceneTransitionContext context, CancellationToken cancelToken)
        {
            worldPresenter.OnEnter();
            return UniTask.CompletedTask;
        }
    }
}
