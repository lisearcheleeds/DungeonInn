using Cysharp.Threading.Tasks;
using DungeonInn.Application.GameLoop;
using DungeonInn.View.Scene.MainScene.World;
using LighthouseExtends.InputLayer;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonInn.Input.Layer
{
    public sealed class WorldSceneInputLayer : IInputLayer
    {
        readonly InputAction togglePauseAction;
        readonly InputAction showInnStatusAction;
        readonly InputAction worldCameraMoveAction;
        readonly InputAction worldCameraLookAction;
        readonly InputAction worldCameraRotateAction;
        readonly InputAction worldCameraZoomAction;
        readonly InputAction previousWorldLayerAction;
        readonly InputAction nextWorldLayerAction;
        readonly ToggleGamePauseUseCase toggleGamePauseUseCase;
        readonly GetInnEconomyStatusUseCase getInnEconomyStatusUseCase;
        readonly WorldCameraController worldCameraController;
        readonly WorldLayerViewController worldLayerViewController;

        public WorldSceneInputLayer(
            InputActions inputActions,
            ToggleGamePauseUseCase toggleGamePauseUseCase,
            GetInnEconomyStatusUseCase getInnEconomyStatusUseCase,
            WorldCameraController worldCameraController,
            WorldLayerViewController worldLayerViewController)
        {
            togglePauseAction = inputActions.Scene.Get().FindAction("TogglePause", true);
            showInnStatusAction = inputActions.Scene.Get().FindAction("ShowInnStatus", true);
            worldCameraMoveAction = inputActions.Scene.Get().FindAction("WorldCameraMove", true);
            worldCameraLookAction = inputActions.Scene.Get().FindAction("WorldCameraLook", true);
            worldCameraRotateAction = inputActions.Scene.Get().FindAction("WorldCameraRotate", true);
            worldCameraZoomAction = inputActions.Scene.Get().FindAction("WorldCameraZoom", true);
            previousWorldLayerAction = inputActions.Scene.Get().FindAction("PreviousWorldLayer", true);
            nextWorldLayerAction = inputActions.Scene.Get().FindAction("NextWorldLayer", true);
            this.toggleGamePauseUseCase = toggleGamePauseUseCase;
            this.getInnEconomyStatusUseCase = getInnEconomyStatusUseCase;
            this.worldCameraController = worldCameraController;
            this.worldLayerViewController = worldLayerViewController;
        }

        public bool BlocksAllInput => false;

        public bool OnActionStarted(InputAction.CallbackContext callbackContext)
        {
            if (callbackContext.action.id == worldCameraRotateAction.id)
            {
                worldCameraController.SetRotating(true);
                return true;
            }

            return false;
        }

        public bool OnActionPerformed(InputAction.CallbackContext callbackContext)
        {
            if (callbackContext.action.id == worldCameraMoveAction.id)
            {
                worldCameraController.SetMoveInput(callbackContext.ReadValue<Vector2>());
                return true;
            }

            if (callbackContext.action.id == worldCameraLookAction.id)
            {
                worldCameraController.AddLookDelta(callbackContext.ReadValue<Vector2>());
                return true;
            }

            if (callbackContext.action.id == worldCameraRotateAction.id)
            {
                worldCameraController.SetRotating(true);
                return true;
            }

            if (callbackContext.action.id == worldCameraZoomAction.id)
            {
                worldCameraController.AddZoomDelta(callbackContext.ReadValue<Vector2>());
                return true;
            }

            if (callbackContext.action.id == previousWorldLayerAction.id)
            {
                worldLayerViewController.SelectPreviousLayer();
                return true;
            }

            if (callbackContext.action.id == nextWorldLayerAction.id)
            {
                worldLayerViewController.SelectNextLayer();
                return true;
            }

            if (callbackContext.action.id == togglePauseAction.id)
            {
                TogglePauseAsync().Forget();
                return true;
            }

            if (callbackContext.action.id == showInnStatusAction.id)
            {
                if (!getInnEconomyStatusUseCase.CanExecute)
                {
                    return true;
                }

                ShowInnStatusAsync().Forget();
                return true;
            }

            return false;
        }

        public bool OnActionCanceled(InputAction.CallbackContext callbackContext)
        {
            if (callbackContext.action.id == worldCameraMoveAction.id)
            {
                worldCameraController.SetMoveInput(Vector2.zero);
                return true;
            }

            if (callbackContext.action.id == worldCameraRotateAction.id)
            {
                worldCameraController.SetRotating(false);
                return true;
            }

            return false;
        }

        async UniTask TogglePauseAsync()
        {
            var timeState = await toggleGamePauseUseCase.ExecuteAsync();
            Debug.Log(timeState.IsPaused ? "[Time] Paused" : "[Time] Resumed");
        }

        async UniTask ShowInnStatusAsync()
        {
            var status = await getInnEconomyStatusUseCase.ExecuteAsync();
            Debug.Log(
                $"[InnStatus] Day={status.CurrentDay} Guests={status.GuestsToday} " +
                $"Demand={status.DemandToday} Rejected={status.RejectedGuestsToday} " +
                $"Occupancy={status.OccupiedRooms}/{status.RoomCapacity} ({status.OccupancyPercent}%) " +
                $"Sales={status.SalesToday}G Satisfaction={status.SatisfactionDeltaToday:+#;-#;0} " +
                $"Reputation={status.Reputation} Treasury={status.GuildGold}G " +
                $"Stock(Sword={status.RookieSwordStock}, Armor={status.RookieArmorStock})");
        }
    }
}
