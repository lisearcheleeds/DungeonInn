using System;
using System.Collections.Generic;
using DungeonInn.Application.World;
using DungeonInn.View.Scene.Bridge;
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
        readonly IActorSelectionCandidateProvider candidateProvider;
        readonly List<ActorViewData> selectionCandidatesBuffer = new();

        public bool HasSelectedActor => actorSelectionService.SelectedActorId.Value.HasValue;

        [Inject]
        public WorldActorSelectionInputHandler(
            InputActions inputActions,
            ActorSelectionService actorSelectionService,
            WorldCameraController worldCameraController,
            LayerPositionViewMapper positionMapper,
            MapLayerViewRegistry layerViewRegistry,
            IActorSelectionCandidateProvider candidateProvider)
        {
            worldActorClickAction = inputActions.Scene.Get().FindAction("WorldActorClick", true);
            worldActorSelectNextAction = inputActions.Scene.Get().FindAction("WorldActorSelectNext", true);
            worldActorSelectPreviousAction = inputActions.Scene.Get().FindAction("WorldActorSelectPrevious", true);
            backAction = inputActions.Scene.Get().FindAction("Back", true);
            this.actorSelectionService = actorSelectionService ?? throw new ArgumentNullException(nameof(actorSelectionService));
            this.worldCameraController = worldCameraController ?? throw new ArgumentNullException(nameof(worldCameraController));
            this.positionMapper = positionMapper ?? throw new ArgumentNullException(nameof(positionMapper));
            this.layerViewRegistry = layerViewRegistry ?? throw new ArgumentNullException(nameof(layerViewRegistry));
            this.candidateProvider = candidateProvider ?? throw new ArgumentNullException(nameof(candidateProvider));
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

            candidateProvider.CopySelectionCandidatesTo(selectionCandidatesBuffer);
            foreach (var candidate in selectionCandidatesBuffer)
            {
                if (!layerViewRegistry.TryGetActorRoot(candidate.Position.LayerId, out var actorRoot))
                {
                    continue;
                }

                var localPosition = positionMapper.ToActorLayerLocalPosition(candidate.Position);
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
                    closestActorId = candidate.ActorId;
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
