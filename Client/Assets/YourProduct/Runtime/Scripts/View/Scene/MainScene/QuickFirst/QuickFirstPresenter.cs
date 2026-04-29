using Cysharp.Threading.Tasks;
using Lighthouse;
using VContainer;
using YourProduct.Core;

namespace YourProduct.View.Scene.MainScene.QuickFirst
{
    public class FirstScenePresenter : IQuickFirstPresenter
    {
        IYourProductSceneManager sceneManager;

        [Inject]
        public void Construct(IYourProductSceneManager sceneManager)
        {
            this.sceneManager = sceneManager;
        }

        UniTask IQuickFirstPresenter.HelloWorld()
        {
            LHLogger.Log("HelloWorld");
            return UniTask.CompletedTask;
        }
    }
}
