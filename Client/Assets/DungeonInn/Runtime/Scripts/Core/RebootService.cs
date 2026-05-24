using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Lighthouse.Scene;
using VContainer;

namespace DungeonInn.Core
{
    public sealed class RebootService : IRebootService, IRebootCleanupRegistry
    {
        static readonly string LauncherSceneName = "Launcher";

        readonly ISceneManager sceneManager;
        readonly IEntrySceneTransitionService entrySceneTransitionService;
        readonly List<IRebootCleanupTarget> cleanupTargets = new();
        readonly List<IRebootCleanupTarget> cleanupTargetSnapshot = new();

        [Inject]
        public RebootService(
            ISceneManager sceneManager,
            IEntrySceneTransitionService entrySceneTransitionService)
        {
            this.sceneManager = sceneManager ?? throw new ArgumentNullException(nameof(sceneManager));
            this.entrySceneTransitionService =
                entrySceneTransitionService ?? throw new ArgumentNullException(nameof(entrySceneTransitionService));
        }

        public void Reboot()
        {
            RebootProcess().Forget();
        }

        public void Register(IRebootCleanupTarget target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (!cleanupTargets.Contains(target))
            {
                cleanupTargets.Add(target);
            }
        }

        public void Unregister(IRebootCleanupTarget target)
        {
            if (target == null)
            {
                return;
            }

            cleanupTargets.Remove(target);
        }

        async UniTask RebootProcess()
        {
            CleanupTargets();
            await sceneManager.PreReboot();

            await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(
                LauncherSceneName,
                UnityEngine.SceneManagement.LoadSceneMode.Single).ToUniTask();
            await entrySceneTransitionService.TransitionToEntrySceneAsync();
        }

        void CleanupTargets()
        {
            cleanupTargetSnapshot.Clear();
            for (var index = 0; index < cleanupTargets.Count; index++)
            {
                cleanupTargetSnapshot.Add(cleanupTargets[index]);
            }

            for (var index = 0; index < cleanupTargetSnapshot.Count; index++)
            {
                cleanupTargetSnapshot[index].CleanupBeforeReboot();
            }

            cleanupTargetSnapshot.Clear();
        }
    }
}
