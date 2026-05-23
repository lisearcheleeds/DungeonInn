using System;
using DungeonInn.Application.World;
using DungeonInn.View.Scene.MainScene.World;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace DungeonInn.Input.Layer
{
    public sealed class WorldActorSelectionInputHandler
    {
        const float SelectionScreenRadius = 50f;

        readonly InputAction worldActorClickAction;
        readonly InputAction worldActorSelectNextAction;
        readonly InputAction worldActorSelectPreviousAction;
        readonly InputAction backAction;
        readonly ActorSelectionService actorSelectionService;
        readonly WorldCameraController worldCameraController;
        readonly LayerPositionViewMapper positionMapper;
        readonly MapLayerViewRegistry layerViewRegistry;
        // M8: IGameWorldStateReader 直接依存 + GetOrCreateActorRoot 副作用は設計違反（T-2）。Actor 選択用 narrow provider に置き換える
        readonly IGameWorldStateReader worldState;

        public bool HasSelectedActor => actorSelectionService.SelectedActorId.Value.HasValue;

        [Inject]
        public WorldActorSelectionInputHandler(
            InputActions inputActions,
            ActorSelectionService actorSelectionService,
            WorldCameraController worldCameraController,
            LayerPositionViewMapper positionMapper,
            MapLayerViewRegistry layerViewRegistry,
            IGameWorldStateReader worldState)
        {
            worldActorClickAction = inputActions.Scene.Get().FindAction("WorldActorClick", true);
            worldActorSelectNextAction = inputActions.Scene.Get().FindAction("WorldActorSelectNext", true);
            worldActorSelectPreviousAction = inputActions.Scene.Get().FindAction("WorldActorSelectPrevious", true);
            backAction = inputActions.Scene.Get().FindAction("Back", true);
            this.actorSelectionService = actorSelectionService ?? throw new ArgumentNullException(nameof(actorSelectionService));
            this.worldCameraController = worldCameraController ?? throw new ArgumentNullException(nameof(worldCameraController));
            this.positionMapper = positionMapper ?? throw new ArgumentNullException(nameof(positionMapper));
            this.layerViewRegistry = layerViewRegistry ?? throw new ArgumentNullException(nameof(layerViewRegistry));
            this.worldState = worldState ?? throw new ArgumentNullException(nameof(worldState));
        }

        public bool HandleActionPerformed(InputAction.CallbackContext callbackContext)
        {
            if (callbackContext.action.id == worldActorClickAction.id)
            {
                HandleClick(callbackContext);
                return true;
            }

            if (callbackContext.action.id == worldActorSelectNextAction.id)
            {
                if (!HasSelectedActor)
                {
                    return false;
                }

                actorSelectionService.SelectNext();
                return true;
            }

            if (callbackContext.action.id == worldActorSelectPreviousAction.id)
            {
                if (!HasSelectedActor)
                {
                    return false;
                }

                actorSelectionService.SelectPrevious();
                return true;
            }

            if (callbackContext.action.id == backAction.id)
            {
                if (!HasSelectedActor)
                {
                    return false;
                }

                actorSelectionService.Deselect();
                return true;
            }

            return false;
        }

        void HandleClick(InputAction.CallbackContext callbackContext)
        {
            var mouseDevice = callbackContext.control.device as Mouse;
            if (mouseDevice == null)
            {
                return;
            }

            var clickScreenPos = (Vector2)mouseDevice.position.ReadValue();
            var closestActorId = Guid.Empty;
            var closestDistance = SelectionScreenRadius;

            foreach (var actor in worldState.Actors)
            {
                var localPosition = positionMapper.ToActorLayerLocalPosition(actor.Position);
                var actorRoot = layerViewRegistry.GetOrCreateActorRoot(actor.Position.LayerId);
                var worldPosition = actorRoot.TransformPoint(localPosition);
                var screenPosition = worldCameraController.WorldToScreenPoint(worldPosition);

                if (screenPosition.z < 0f)
                {
                    continue;
                }

                var screenPos2d = new Vector2(screenPosition.x, screenPosition.y);
                var distance = Vector2.Distance(clickScreenPos, screenPos2d);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestActorId = actor.Id;
                }
            }

            if (closestActorId == Guid.Empty)
            {
                actorSelectionService.Deselect();
            }
            else if (actorSelectionService.SelectedActorId.Value == closestActorId)
            {
                actorSelectionService.Deselect();
            }
            else
            {
                actorSelectionService.Select(closestActorId);
            }
        }
    }
}
