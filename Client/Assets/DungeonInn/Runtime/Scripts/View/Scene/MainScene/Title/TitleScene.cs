using System.Threading;
using Cysharp.Threading.Tasks;
using DungeonInn.LighthouseGenerated;
using DungeonInn.View.Base;
using Lighthouse.Scene;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.Title
{
    public sealed class TitleScene : ProductMainSceneBase<TitleScene.TitleTransitionData>
    {
        ITitlePresenter titlePresenter;

        public override MainSceneId MainSceneId => DungeonInnMainSceneId.Title;

        public sealed class TitleTransitionData : ProductTransitionDataBase
        {
            public override MainSceneId MainSceneId => DungeonInnMainSceneId.Title;
        }

        [Inject]
        public void Construct(ITitlePresenter titlePresenter)
        {
            this.titlePresenter = titlePresenter;
        }

        protected override UniTask OnSetup()
        {
            titlePresenter.Setup();
            return UniTask.CompletedTask;
        }

        protected override UniTask OnEnter(TitleTransitionData transitionData, ISceneTransitionContext context, CancellationToken cancelToken)
        {
            titlePresenter.OnEnter();
            return UniTask.CompletedTask;
        }
    }
}
