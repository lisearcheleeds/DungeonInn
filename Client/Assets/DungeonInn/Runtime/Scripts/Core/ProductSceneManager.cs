using System;
using Cysharp.Threading.Tasks;
using Lighthouse.Scene;
using UnityEngine;
using VContainer;

namespace DungeonInn.Core
{
    /// <summary>
    /// Wraps Lighthouse's SceneManager and catches unhandled transition exceptions.
    /// When an unexpected exception occurs, it logs the error and reboots the application
    /// rather than attempting partial recovery, which could leave the app in an inconsistent state.
    /// </summary>
    public sealed class ProductSceneManager : IProductSceneManager
    {
        readonly ISceneManager sceneManager;
        readonly IRebootService rebootService;
        readonly IGameSessionLifecycle gameSessionLifecycle;

        public bool IsTransition => sceneManager.IsTransition;

        [Inject]
        public ProductSceneManager(
            ISceneManager sceneManager,
            IRebootService rebootService,
            IGameSessionLifecycle gameSessionLifecycle)
        {
            this.sceneManager = sceneManager;
            this.rebootService = rebootService;
            this.gameSessionLifecycle = gameSessionLifecycle;
        }

        async UniTask IProductSceneManager.TransitionScene(
            TransitionDataBase nextTransitionData,
            TransitionType transitionType,
            MainSceneId backMainSceneId)
        {
            try
            {
                await sceneManager.TransitionScene(nextTransitionData, transitionType, backMainSceneId);
                EndSessionIfSessionExit(nextTransitionData.MainSceneId);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                gameSessionLifecycle.EndSession();
                Debug.LogError($"[ProductSceneManager] Unhandled exception during transition. Rebooting.\n{exception}");
                rebootService.Reboot();
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
            catch (Exception exception)
            {
                gameSessionLifecycle.EndSession();
                Debug.LogError($"[ProductSceneManager] Unhandled exception during back transition. Rebooting.\n{exception}");
                rebootService.Reboot();
            }
        }

        UniTask IProductSceneManager.PreReboot() => sceneManager.PreReboot();

        void EndSessionIfSessionExit(MainSceneId nextMainSceneId)
        {
            if (gameSessionLifecycle.IsSessionScene(nextMainSceneId))
            {
                return;
            }

            gameSessionLifecycle.EndSession();
        }
    }
}
