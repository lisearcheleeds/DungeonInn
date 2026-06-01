using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.World;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Map;
using DungeonInn.Master;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldActorPresenter
    {
        readonly IActorViewDataProvider viewDataProvider;
        readonly IActorStatusViewDataProvider activeActorProvider;
        readonly LayerPositionViewMapper positionMapper;
        readonly WorldActorViewRegistry actorViewRegistry;
        readonly ActorVisualDefinitionLoader visualDefinitionLoader;
        readonly WorldCameraController worldCameraController;
        readonly ActorCombatAnimationPresenter combatAnimationPresenter;
        readonly Action<Guid, ActorView> updateActorViewAction;
        readonly Dictionary<Guid, ActorViewData> actorViewDataById = new();
        readonly Dictionary<Guid, string> appliedVisualIds = new();
        readonly HashSet<ActorVisualRequestKey> requestedVisuals = new();
        readonly HashSet<Guid> walkingActorsThisFrame = new();
        readonly List<ActorViewData> activeActorBuffer = new();
        float frameYawDegrees;
        float frameDeltaTime;
        Quaternion frameCameraRotation;

        [Inject]
        public WorldActorPresenter(
            IActorViewDataProvider viewDataProvider,
            IActorStatusViewDataProvider activeActorProvider,
            LayerPositionViewMapper positionMapper,
            WorldActorViewRegistry actorViewRegistry,
            ActorVisualDefinitionLoader visualDefinitionLoader,
            WorldCameraController worldCameraController,
            ActorCombatAnimationPresenter combatAnimationPresenter)
        {
            this.viewDataProvider = viewDataProvider ?? throw new ArgumentNullException(nameof(viewDataProvider));
            this.activeActorProvider = activeActorProvider ?? throw new ArgumentNullException(nameof(activeActorProvider));
            this.positionMapper = positionMapper ?? throw new ArgumentNullException(nameof(positionMapper));
            this.actorViewRegistry = actorViewRegistry ?? throw new ArgumentNullException(nameof(actorViewRegistry));
            this.visualDefinitionLoader =
                visualDefinitionLoader ?? throw new ArgumentNullException(nameof(visualDefinitionLoader));
            this.worldCameraController = worldCameraController ?? throw new ArgumentNullException(nameof(worldCameraController));
            this.combatAnimationPresenter =
                combatAnimationPresenter ?? throw new ArgumentNullException(nameof(combatAnimationPresenter));
            updateActorViewAction = UpdateSingleActorView;
        }

        public void UpdateVisuals()
        {
            frameYawDegrees = worldCameraController.CurrentYawDegrees;
            frameCameraRotation = worldCameraController.CurrentCameraRotation;
            frameDeltaTime = Time.unscaledDeltaTime;
            var changes = viewDataProvider.ConsumeChanges();
            walkingActorsThisFrame.Clear();

            foreach (var actorId in changes.RemovedActorIds)
            {
                actorViewDataById.Remove(actorId);
                appliedVisualIds.Remove(actorId);
                combatAnimationPresenter.RemoveActor(actorId);
                actorViewRegistry.RemoveActorObject(actorId);
            }

            foreach (var actor in changes.ChangedActors)
            {
                ApplyActorViewData(actor, forcePosition: false);
            }

            actorViewRegistry.ForEachActorView(updateActorViewAction);
        }

        public void RefreshLayerActors(MapLayerId layerId)
        {
            activeActorProvider.CopyActiveActorsTo(activeActorBuffer);
            for (var i = 0; i < activeActorBuffer.Count; i++)
            {
                var actor = activeActorBuffer[i];
                if (actor.Position.LayerId.Equals(layerId))
                {
                    ApplyActorViewData(actor, forcePosition: true);
                }
            }
        }

        void ApplyActorViewData(ActorViewData actor, bool forcePosition)
        {
            var actorView = actorViewRegistry.GetOrCreateActorView(
                actor.ActorId,
                actor.Position,
                out var created);
            actorViewDataById[actor.ActorId] = actor;
            RequestVisualIfNeeded(actor.VisualId);

            var positionChanged = forcePosition ||
                created ||
                !actorView.HasLastPosition ||
                !IsSamePosition(actorView.LastPosition, actor.Position);
            if (!positionChanged)
            {
                return;
            }

            if (forcePosition ||
                !created && actorView.HasLastPosition && !actorView.LastPosition.LayerId.Equals(actor.Position.LayerId))
            {
                actorViewRegistry.SetActorLayer(actorView, actor.Position);
            }

            actorView.SetLocalPosition(ResolveActorLocalPosition(actor));
            actorView.UpdateFacing(actor.Position);
            walkingActorsThisFrame.Add(actor.ActorId);
        }

        void UpdateSingleActorView(Guid actorId, ActorView actorView)
        {
            var isWalking = walkingActorsThisFrame.Contains(actorId);
            if (actorViewDataById.TryGetValue(actorId, out var viewData) &&
                TryApplyLoadedVisual(actorId, actorView, viewData))
            {
                actorView.SetLocalPosition(ResolveActorLocalPosition(viewData));
            }

            var isVisible = worldCameraController.IsWorldPositionVisible(
                actorView.transform.position,
                worldCameraController.ActorViewportMargin) && viewData.IsVisible;
            actorView.SetVisible(isVisible);
            if (!isVisible)
            {
                return;
            }

            var direction = ComputeDirection(actorView.Facing, frameYawDegrees);
            var animState = ResolveAnimationState(actorId, actorView, isWalking, direction);
            actorView.SetAnimationState(animState);

            actorView.Tick(frameDeltaTime, direction);
            actorView.SetBillboardRotation(frameCameraRotation);
            actorView.SetFlip(false);
        }

        ActorAnimationState ResolveAnimationState(
            Guid actorId,
            ActorView actorView,
            bool isWalking,
            ActorAnimationDirection direction)
        {
            if (combatAnimationPresenter.TryGetOverride(actorId, out var overrideState))
            {
                if (overrideState == ActorAnimationState.Damage &&
                    actorView.IsDamageOneShotComplete(direction))
                {
                    combatAnimationPresenter.ClearDamageOverride(actorId);
                    return isWalking ? ActorAnimationState.Walk : ActorAnimationState.Idle;
                }

                return overrideState;
            }

            return isWalking ? ActorAnimationState.Walk : ActorAnimationState.Idle;
        }

        Vector3 ResolveActorLocalPosition(ActorViewData actor)
        {
            var sizeTier = ActorVisualSizeTier.AdventurerS;
            if (visualDefinitionLoader.TryGetLoaded(
                    actor.VisualId,
                    GameConstants.DefaultActorSkinId,
                    out var definition))
            {
                sizeTier = definition.VisualSizeTier;
            }

            var groundAnchorOffset = ActorVisualSizeTierCatalog.GetGroundAnchorOffsetMeters(sizeTier);
            return positionMapper.ToActorLayerLocalPosition(actor.Position) + Vector3.up * groundAnchorOffset;
        }

        void RequestVisualIfNeeded(string visualId)
        {
            var key = new ActorVisualRequestKey(visualId, GameConstants.DefaultActorSkinId);
            if (!requestedVisuals.Add(key))
            {
                return;
            }

            visualDefinitionLoader.RequestLoadAsync(
                    visualId,
                    GameConstants.DefaultActorSkinId,
                    default)
                .Forget();
        }

        bool TryApplyLoadedVisual(Guid actorId, ActorView actorView, ActorViewData viewData)
        {
            if (!visualDefinitionLoader.TryGetLoaded(
                    viewData.VisualId,
                    GameConstants.DefaultActorSkinId,
                    out var definition))
            {
                return false;
            }

            if (appliedVisualIds.TryGetValue(actorId, out var appliedVisualId) &&
                appliedVisualId == viewData.VisualId)
            {
                return true;
            }

            actorView.ApplyVisual(definition);
            appliedVisualIds[actorId] = viewData.VisualId;
            return true;
        }

        static bool IsSamePosition(DungeonInn.Domain.Map.LayerPosition first, DungeonInn.Domain.Map.LayerPosition second)
        {
            return first.LayerId.Equals(second.LayerId) &&
                Mathf.Approximately(first.X, second.X) &&
                Mathf.Approximately(first.Z, second.Z);
        }

        static ActorAnimationDirection ComputeDirection(Vector2 facing, float cameraYawDegrees)
        {
            var yawRotation = Quaternion.Euler(0f, cameraYawDegrees, 0f);
            var cameraRight = yawRotation * Vector3.right;
            var cameraForward = yawRotation * Vector3.forward;
            var facing3 = new Vector3(facing.x, 0f, facing.y);
            var rightDot = Vector3.Dot(cameraRight, facing3);
            var forwardDot = Vector3.Dot(cameraForward, facing3);
            var epsilon = -0.01f;
            if (epsilon <= forwardDot)
            {
                return epsilon <= rightDot ? ActorAnimationDirection.NE : ActorAnimationDirection.NW;
            }

            return epsilon <= rightDot ? ActorAnimationDirection.SE : ActorAnimationDirection.SW;
        }
    }
}
