using System;
using Cysharp.Threading.Tasks;
using DungeonInn.View.Scene.MainScene.Title;
using Lighthouse.Scene;
using VContainer;

namespace DungeonInn.Core
{
    public sealed class EntrySceneTransitionService : IEntrySceneTransitionService
    {
        static readonly string LauncherSceneName = "Launcher";

        readonly ISceneManager sceneManager;

        [Inject]
        public EntrySceneTransitionService(ISceneManager sceneManager)
        {
            this.sceneManager = sceneManager ?? throw new ArgumentNullException(nameof(sceneManager));
        }

        public async UniTask TransitionToEntrySceneAsync()
        {
            await sceneManager.TransitionScene(new TitleScene.TitleTransitionData());

            if (!string.IsNullOrEmpty(UnityEngine.SceneManagement.SceneManager.GetSceneByName(LauncherSceneName).name))
            {
                await UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(LauncherSceneName).ToUniTask();
            }
        }
    }
}
