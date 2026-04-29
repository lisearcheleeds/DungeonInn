using System;
using Cysharp.Threading.Tasks;
using Lighthouse.Scene;
using UnityEngine;
using VContainer;

namespace DungeonInn.Runtime.Scripts.Core
{
    /// <summary>
    /// Wraps Lighthouse's SceneManager and catches unhandled transition exceptions.
    /// When an unexpected exception occurs, it logs the error and reboots the application
    /// rather than attempting partial recovery, which could leave the app in an inconsistent state.
    /// </summary>
    public sealed class ProductSceneManager : IProductSceneManager
    {
        readonly ISceneManager sceneManager;
        readonly ILauncher launcher;

        public bool IsTransition => sceneManager.IsTransition;

        [Inject]
        public ProductSceneManager(ISceneManager sceneManager, ILauncher launcher)
        {
            this.sceneManager = sceneManager;
            this.launcher = launcher;
        }

        async UniTask IProductSceneManager.TransitionScene(
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
                Debug.LogError($"[ProductSceneManager] Unhandled exception during transition. Rebooting.\n{e}");
                launcher.Reboot();
            }
        }

        async UniTask IProductSceneManager.BackScene(TransitionType transitionType)
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
                Debug.LogError($"[ProductSceneManager] Unhandled exception during back transition. Rebooting.\n{e}");
                launcher.Reboot();
            }
        }

        UniTask IProductSceneManager.PreReboot() => sceneManager.PreReboot();
    }
}
