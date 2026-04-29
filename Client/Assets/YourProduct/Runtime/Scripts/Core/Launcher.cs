using Cysharp.Threading.Tasks;
using Lighthouse.Scene;
using UnityEngine;
using VContainer;
using YourProduct.View.Scene.MainScene.QuickFirst;

namespace YourProduct.Core
{
    public sealed class Launcher : ILauncher
    {
        static readonly string LauncherSceneName = "Launcher";

        readonly ISceneManager sceneManager;

        [Inject]
        public Launcher(ISceneManager sceneManager)
        {
            this.sceneManager = sceneManager;
        }

        void ILauncher.Reboot()
        {
            RebootProcess().Forget();

            async UniTask RebootProcess()
            {
                await sceneManager.PreReboot();

                await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(LauncherSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
                await LaunchProcess();

                TransitionNextScene();
            }
        }

        async UniTask ILauncher.Launch()
        {
            await FirstLaunchProcess();
            await LaunchProcess();
            TransitionNextScene();
        }

        UniTask FirstLaunchProcess()
        {
            return UniTask.CompletedTask;
        }

        UniTask LaunchProcess()
        {
            return UniTask.CompletedTask;
        }

        void TransitionNextScene()
        {
            UniTask.Void(async () =>
            {
                await sceneManager.TransitionScene(new FirstSceneScene.QuickFirstTransitionData());

                if (!string.IsNullOrEmpty(UnityEngine.SceneManagement.SceneManager.GetSceneByName(LauncherSceneName).name))
                {
                    await UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(LauncherSceneName);
                }
            });
        }
    }
}
