using System;
using DungeonInn.Application.Economy;
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
        readonly WorldActorSelectionInputHandler actorSelectionInputHandler;

        public WorldSceneInputLayer(
            InputActions inputActions,
            ToggleGamePauseUseCase toggleGamePauseUseCase,
            GetInnEconomyStatusUseCase getInnEconomyStatusUseCase,
            WorldCameraController worldCameraController,
            WorldLayerViewController worldLayerViewController,
            WorldActorSelectionInputHandler actorSelectionInputHandler)
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
            this.actorSelectionInputHandler = actorSelectionInputHandler ?? throw new ArgumentNullException(nameof(actorSelectionInputHandler));
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
            if (actorSelectionInputHandler.HandleActionPerformed(callbackContext))
            {
                return true;
            }

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
                TogglePause();
                return true;
            }

            if (callbackContext.action.id == showInnStatusAction.id)
            {
                if (!getInnEconomyStatusUseCase.CanExecute)
                {
                    return true;
                }

                ShowInnStatus();
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

        void TogglePause()
        {
            var timeState = toggleGamePauseUseCase.Execute();
            Debug.Log(timeState.IsPaused ? "[Time] Paused" : "[Time] Resumed");
        }

        void ShowInnStatus()
        {
            var status = getInnEconomyStatusUseCase.Execute();
            var current = status.Current;
            Debug.Log(
                $"[InnStatus] Day={status.CurrentDay} Guests={current.Guests} " +
                $"Demand={current.Demand} Rejected={current.RejectedGuests} " +
                $"Occupancy={current.OccupiedRooms}/{current.RoomCapacity} ({current.OccupancyPercent}%) " +
                $"Sales={current.Sales}G Satisfaction={current.SatisfactionDelta:+#;-#;0} " +
                $"Reputation={current.Reputation} Treasury={current.GuildGold}G " +
                $"Stock(Sword={current.RookieSwordStock}, Armor={current.RookieArmorStock})");
        }
    }
}
