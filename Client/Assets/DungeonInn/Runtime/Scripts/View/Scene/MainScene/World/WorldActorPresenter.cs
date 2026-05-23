using System;
using System.Collections.Generic;
using DungeonInn.Application.World;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldActorPresenter
    {
        readonly IActorViewDataProvider viewDataProvider;
        readonly LayerPositionViewMapper positionMapper;
        readonly WorldActorViewRegistry actorViewRegistry;
        readonly ActorSpriteVisualConfig actorSpriteVisualConfig;
        readonly WorldCameraController worldCameraController;
        readonly ActorCombatAnimationPresenter combatAnimationPresenter;
        readonly Action<Guid, ActorView> updateActorViewAction;
        readonly Dictionary<Guid, ActorBehaviorType> actorBehaviorTypes = new();
        readonly HashSet<Guid> walkingActorsThisFrame = new();
        float frameYawDegrees;
        float frameDeltaTime;
        Quaternion frameCameraRotation;

        [Inject]
        public WorldActorPresenter(
            IActorViewDataProvider viewDataProvider,
            LayerPositionViewMapper positionMapper,
            WorldActorViewRegistry actorViewRegistry,
            ActorSpriteVisualConfig actorSpriteVisualConfig,
            WorldCameraController worldCameraController,
            ActorCombatAnimationPresenter combatAnimationPresenter)
        {
            this.viewDataProvider = viewDataProvider ?? throw new ArgumentNullException(nameof(viewDataProvider));
            this.positionMapper = positionMapper ?? throw new ArgumentNullException(nameof(positionMapper));
            this.actorViewRegistry = actorViewRegistry ?? throw new ArgumentNullException(nameof(actorViewRegistry));
            this.actorSpriteVisualConfig = actorSpriteVisualConfig ?? throw new ArgumentNullException(nameof(actorSpriteVisualConfig));
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
                actorBehaviorTypes.Remove(actorId);
                combatAnimationPresenter.RemoveActor(actorId);
                actorViewRegistry.RemoveActorObject(actorId);
            }

            foreach (var actor in changes.ChangedActors)
            {
                var actorView = actorViewRegistry.GetOrCreateActorView(
                    actor.ActorId,
                    actor.Position,
                    out var created);
                actorBehaviorTypes[actor.ActorId] = actor.BehaviorType;

                var positionChanged = created ||
                    !actorView.HasLastPosition ||
                    !IsSamePosition(actorView.LastPosition, actor.Position);
                if (positionChanged)
                {
                    if (!created && !actorView.LastPosition.LayerId.Equals(actor.Position.LayerId))
                    {
                        actorViewRegistry.SetActorLayer(actorView, actor.Position);
                    }

                    actorView.SetLocalPosition(ResolveActorLocalPosition(actor));
                    actorView.UpdateFacing(actor.Position);
                    walkingActorsThisFrame.Add(actor.ActorId);
                }
            }

            actorViewRegistry.ForEachActorView(updateActorViewAction);
        }

        void UpdateSingleActorView(Guid actorId, ActorView actorView)
        {
            var isWalking = walkingActorsThisFrame.Contains(actorId);
            var isVisible = worldCameraController.IsWorldPositionVisible(
                actorView.transform.position,
                worldCameraController.ActorViewportMargin);
            actorView.SetVisible(isVisible);
            if (!isVisible)
            {
                return;
            }

            var animState = ResolveAnimationState(actorId, actorView, isWalking);
            actorView.SetAnimationState(animState);
            actorView.Tick(frameDeltaTime);

            var direction = ComputeDirection(actorView.Facing, frameYawDegrees);
            var behaviorType = actorBehaviorTypes.TryGetValue(actorId, out var value)
                ? value
                : ActorBehaviorType.None;
            var isCombatAnimState = animState == ActorAnimationState.Combat ||
                animState == ActorAnimationState.Hit ||
                animState == ActorAnimationState.Dead;
            var sprite = actorSpriteVisualConfig.GetSprite(
                behaviorType,
                direction,
                !isCombatAnimState && isWalking,
                actorView.CurrentFrameIndex);
            var sizeTier = actorSpriteVisualConfig.GetVisualSizeTier(behaviorType);
            actorView.SetSprite(sprite);
            actorView.SetVisualCanvasHeight(ActorVisualSizeTierCatalog.GetCanvasHeightMeters(sizeTier));
            actorView.SetBillboardRotation(frameCameraRotation);
            actorView.SetFlip(false);
        }

        ActorAnimationState ResolveAnimationState(Guid actorId, ActorView actorView, bool isWalking)
        {
            if (combatAnimationPresenter.TryGetOverride(actorId, out var overrideState))
            {
                if (overrideState == ActorAnimationState.Hit && actorView.IsHitOneShotComplete)
                {
                    combatAnimationPresenter.ClearHitOverride(actorId);
                    return isWalking ? ActorAnimationState.Walk : ActorAnimationState.Idle;
                }

                return overrideState;
            }

            return isWalking ? ActorAnimationState.Walk : ActorAnimationState.Idle;
        }

        Vector3 ResolveActorLocalPosition(ActorViewData actor)
        {
            var sizeTier = actorSpriteVisualConfig.GetVisualSizeTier(actor.BehaviorType);
            var groundAnchorOffset = ActorVisualSizeTierCatalog.GetGroundAnchorOffsetMeters(sizeTier);
            return positionMapper.ToActorLayerLocalPosition(actor.Position) + Vector3.up * groundAnchorOffset;
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
            if (0f <= forwardDot)
            {
                return 0f <= rightDot ? ActorAnimationDirection.NE : ActorAnimationDirection.NW;
            }

            return 0f <= rightDot ? ActorAnimationDirection.SE : ActorAnimationDirection.SW;
        }
    }
}
