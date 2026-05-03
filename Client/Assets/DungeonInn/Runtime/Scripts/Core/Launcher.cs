using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Character;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Inn;
using DungeonInn.Domain.World;
using DungeonInn.Runtime.Scripts.View.Scene.MainScene.World;
using Lighthouse.Scene;
using UnityEngine.SceneManagement;
using VContainer;

namespace DungeonInn.Runtime.Scripts.Core
{
    public sealed class Launcher : ILauncher
    {
        static readonly string LauncherSceneName = "Launcher";

        readonly ISceneManager sceneManager;
        readonly IWorldConfigRepository worldConfigRepository;
        readonly IInnConfigRepository innConfigRepository;
        readonly IAdventurerConfigRepository adventurerConfigRepository;
        readonly IMonsterConfigRepository monsterConfigRepository;
        readonly IDungeonConfigRepository dungeonConfigRepository;

        [Inject]
        public Launcher(
            ISceneManager sceneManager,
            IWorldConfigRepository worldConfigRepository,
            IInnConfigRepository innConfigRepository,
            IAdventurerConfigRepository adventurerConfigRepository,
            IMonsterConfigRepository monsterConfigRepository,
            IDungeonConfigRepository dungeonConfigRepository)
        {
            this.sceneManager = sceneManager;
            this.worldConfigRepository = worldConfigRepository;
            this.innConfigRepository = innConfigRepository;
            this.adventurerConfigRepository = adventurerConfigRepository;
            this.monsterConfigRepository = monsterConfigRepository;
            this.dungeonConfigRepository = dungeonConfigRepository;
        }

        void ILauncher.Reboot()
        {
            RebootProcess().Forget();

            async UniTask RebootProcess()
            {
                await sceneManager.PreReboot();
                await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(
                    LauncherSceneName,
                    UnityEngine.SceneManagement.LoadSceneMode.Single).ToUniTask();
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

        async UniTask LaunchProcess()
        {
            await UniTask.WhenAll(
                worldConfigRepository.LoadAsync(),
                innConfigRepository.LoadAsync(),
                adventurerConfigRepository.LoadAsync(),
                monsterConfigRepository.LoadAsync(),
                dungeonConfigRepository.LoadAsync()
            );
        }

        void TransitionNextScene()
        {
            UniTask.Void(async () =>
            {
                try
                {
                    await sceneManager.TransitionScene(new WorldScene.WorldTransitionData());

                    if (!string.IsNullOrEmpty(UnityEngine.SceneManagement.SceneManager.GetSceneByName(LauncherSceneName).name))
                    {
                        await UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(LauncherSceneName).ToUniTask();
                    }
                }
                catch (System.OperationCanceledException)
                {
                    throw;
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogError($"[Launcher] TransitionScene failed: {e}");
                    ((ILauncher)this).Reboot();
                }
            });
        }
    }
}
