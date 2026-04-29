using System;
using Cysharp.Threading.Tasks;
using Lighthouse.Scene;
using UnityEngine;
using VContainer;

namespace YourProduct.Core
{
    /// <summary>
    /// Wraps Lighthouse's SceneManager and catches unhandled transition exceptions.
    /// When an unexpected exception occurs, it logs the error and reboots the application
    /// rather than attempting partial recovery, which could leave the app in an inconsistent state.
    /// </summary>
    public sealed class YourProductSceneManager : IYourProductSceneManager
    {
        readonly ISceneManager sceneManager;
        readonly ILauncher launcher;

        public bool IsTransition => sceneManager.IsTransition;

        [Inject]
        public YourProductSceneManager(ISceneManager sceneManager, ILauncher launcher)
        {
            this.sceneManager = sceneManager;
            this.launcher = launcher;
        }

        async UniTask IYourProductSceneManager.TransitionScene(
            TransitionDataBase nextTransitionData,
            TransitionType transitionType,
            MainSceneId backMainSceneId)
        {
            try
            {
                await sceneManager.TransitionScene(nextTransitionData, transitionType, backMainSceneId);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[YourProductSceneManager] Unhandled exception during transition. Rebooting.\n{e}");
                launcher.Reboot();
            }
        }

        async UniTask IYourProductSceneManager.BackScene(TransitionType transitionType)
        {
            try
            {
                await sceneManager.BackScene(transitionType);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[YourProductSceneManager] Unhandled exception during back transition. Rebooting.\n{e}");
                launcher.Reboot();
            }
        }

        UniTask IYourProductSceneManager.PreReboot() => sceneManager.PreReboot();
    }
}
