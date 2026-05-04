using Cysharp.Threading.Tasks;
using DungeonInn.Core;
using Lighthouse;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.QuickFirst
{
    public class FirstScenePresenter : IQuickFirstPresenter
    {
        IProductSceneManager sceneManager;

        [Inject]
        public void Construct(IProductSceneManager sceneManager)
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
