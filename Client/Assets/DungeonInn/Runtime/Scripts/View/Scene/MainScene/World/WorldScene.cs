using System.Threading;
using Cysharp.Threading.Tasks;
using DungeonInn.LighthouseGenerated;
using DungeonInn.View.Base;
using Lighthouse.Scene;
using Lighthouse.Scene.SceneBase;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldScene : MainSceneBase<WorldScene.WorldTransitionData>
    {
        IWorldPresenter worldPresenter;

        public override MainSceneId MainSceneId => DungeonInnMainSceneId.World;

        public sealed class WorldTransitionData : ProductTransitionDataBase
        {
            public override MainSceneId MainSceneId => DungeonInnMainSceneId.World;
        }

        [Inject]
        public void Construct(IWorldPresenter worldPresenter)
        {
            this.worldPresenter = worldPresenter;
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
