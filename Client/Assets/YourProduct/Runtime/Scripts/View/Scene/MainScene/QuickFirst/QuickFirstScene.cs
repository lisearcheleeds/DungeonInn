using Cysharp.Threading.Tasks;
using Lighthouse.Scene;
using VContainer;
using YourProduct.LighthouseGenerated;
using YourProduct.View.Base;

namespace YourProduct.View.Scene.MainScene.QuickFirst
{
    public class FirstSceneScene : ProductCanvasMainSceneBase<FirstSceneScene.QuickFirstTransitionData>
    {
        IQuickFirstPresenter splashPresenter;

        public override MainSceneId MainSceneId => YourProductMainSceneId.FirstScene;

        public class QuickFirstTransitionData : ProductTransitionDataBase
        {
            public override MainSceneId MainSceneId => YourProductMainSceneId.FirstScene;
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
