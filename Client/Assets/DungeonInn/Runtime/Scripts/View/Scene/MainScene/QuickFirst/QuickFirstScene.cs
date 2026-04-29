using Cysharp.Threading.Tasks;
using DungeonInn.LighthouseGenerated;
using DungeonInn.Runtime.Scripts.View.Base;
using Lighthouse.Scene;
using VContainer;

namespace DungeonInn.Runtime.Scripts.View.Scene.MainScene.QuickFirst
{
    public class FirstSceneScene : ProductCanvasMainSceneBase<FirstSceneScene.QuickFirstTransitionData>
    {
        IQuickFirstPresenter splashPresenter;

        public override MainSceneId MainSceneId => DungeonInnMainSceneId.FirstScene;

        public class QuickFirstTransitionData : ProductTransitionDataBase
        {
            public override MainSceneId MainSceneId => DungeonInnMainSceneId.FirstScene;
        }

        [Inject]
        public void Constructor(IQuickFirstPresenter splashPresenter)
        {
            this.splashPresenter = splashPresenter;
        }

        public override void OnSceneTransitionFinished(SceneTransitionDiff sceneTransitionDiff)
        {
            splashPresenter.HelloWorld().Forget();
        }
    }
}
