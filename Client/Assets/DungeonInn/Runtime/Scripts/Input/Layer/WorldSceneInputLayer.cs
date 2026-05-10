using Cysharp.Threading.Tasks;
using DungeonInn.Application.GameLoop;
using LighthouseExtends.InputLayer;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonInn.Input.Layer
{
    public sealed class WorldSceneInputLayer : IInputLayer
    {
        readonly InputAction togglePauseAction;
        readonly ToggleGamePauseUseCase toggleGamePauseUseCase;

        public WorldSceneInputLayer(
            InputActions inputActions,
            ToggleGamePauseUseCase toggleGamePauseUseCase)
        {
            togglePauseAction = inputActions.Scene.Get().FindAction("TogglePause", true);
            this.toggleGamePauseUseCase = toggleGamePauseUseCase;
        }

        public bool BlocksAllInput => false;

        public bool OnActionStarted(InputAction.CallbackContext callbackContext)
        {
            return false;
        }

        public bool OnActionPerformed(InputAction.CallbackContext callbackContext)
        {
            if (callbackContext.action.id != togglePauseAction.id)
            {
                return false;
            }

            TogglePauseAsync().Forget();
            return true;
        }

        public bool OnActionCanceled(InputAction.CallbackContext callbackContext)
        {
            return false;
        }

        async UniTask TogglePauseAsync()
        {
            var timeState = await toggleGamePauseUseCase.ExecuteAsync();
            Debug.Log(timeState.IsPaused ? "[Time] Paused" : "[Time] Resumed");
        }
    }
}
