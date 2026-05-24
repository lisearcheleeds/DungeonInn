using Cysharp.Threading.Tasks;
using VContainer;

namespace DungeonInn.Core
{
    public sealed class Launcher : ILauncher
    {
        readonly IEntrySceneTransitionService entrySceneTransitionService;
        readonly IRebootService rebootService;

        [Inject]
        public Launcher(
            IEntrySceneTransitionService entrySceneTransitionService,
            IRebootService rebootService)
        {
            this.entrySceneTransitionService = entrySceneTransitionService;
            this.rebootService = rebootService;
        }

        void ILauncher.Reboot()
        {
            rebootService.Reboot();
        }

        async UniTask ILauncher.Launch()
        {
            await FirstLaunchProcess();
            await LaunchProcess();
            await entrySceneTransitionService.TransitionToEntrySceneAsync();
        }

        UniTask FirstLaunchProcess()
        {
            return UniTask.CompletedTask;
        }

        UniTask LaunchProcess()
        {
            return UniTask.CompletedTask;
        }

    }
}
