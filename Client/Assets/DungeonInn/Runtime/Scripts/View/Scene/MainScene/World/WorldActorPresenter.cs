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
        readonly Action<Guid, ActorView> updateActorViewAction;
        readonly Dictionary<Guid, ActorBehaviorType> actorBehaviorTypes = new();
        readonly HashSet<Guid> walkingActorsThisFrame = new();
        bool hasLastCameraYawDegrees;
        float lastCameraYawDegrees;
        float frameYawDegrees;
        float frameDeltaTime;
        bool frameYawChanged;

        [Inject]
        public WorldActorPresenter(
            IActorViewDataProvider viewDataProvider,
            LayerPositionViewMapper positionMapper,
            WorldActorViewRegistry actorViewRegistry,
            ActorSpriteVisualConfig actorSpriteVisualConfig,
            WorldCameraController worldCameraController)
        {
            this.viewDataProvider = viewDataProvider ?? throw new ArgumentNullException(nameof(viewDataProvider));
            this.positionMapper = positionMapper ?? throw new ArgumentNullException(nameof(positionMapper));
            this.actorViewRegistry = actorViewRegistry ?? throw new ArgumentNullException(nameof(actorViewRegistry));
            this.actorSpriteVisualConfig = actorSpriteVisualConfig ?? throw new ArgumentNullException(nameof(actorSpriteVisualConfig));
            this.worldCameraController = worldCameraController ?? throw new ArgumentNullException(nameof(worldCameraController));
            updateActorViewAction = UpdateSingleActorView;
        }

        public void UpdateVisuals()
        {
            frameYawDegrees = worldCameraController.CurrentYawDegrees;
            frameYawChanged = !hasLastCameraYawDegrees ||
                !Mathf.Approximately(lastCameraYawDegrees, frameYawDegrees);
            frameDeltaTime = Time.unscaledDeltaTime;
            var changes = viewDataProvider.ConsumeChanges();
            walkingActorsThisFrame.Clear();

            foreach (var actorId in changes.RemovedActorIds)
            {
                actorBehaviorTypes.Remove(actorId);
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

                    actorView.SetLocalPosition(positionMapper.ToActorLayerLocalPosition(actor.Position));
                    actorView.UpdateFacing(actor.Position);
                    walkingActorsThisFrame.Add(actor.ActorId);
                }
            }

            actorViewRegistry.ForEachActorView(updateActorViewAction);

            lastCameraYawDegrees = frameYawDegrees;
            hasLastCameraYawDegrees = true;
        }

        void UpdateSingleActorView(Guid actorId, ActorView actorView)
        {
            var isWalking = walkingActorsThisFrame.Contains(actorId);
            actorView.SetAnimationState(isWalking ? ActorAnimationState.Walk : ActorAnimationState.Idle);
            actorView.Tick(frameDeltaTime);

            var direction = ComputeDirection(actorView.Facing, frameYawDegrees);
            var behaviorType = actorBehaviorTypes.TryGetValue(actorId, out var value)
                ? value
                : ActorBehaviorType.None;
            var sprite = actorSpriteVisualConfig.GetSprite(
                behaviorType,
                direction,
                isWalking,
                actorView.CurrentFrameIndex);
            var sizeTier = actorSpriteVisualConfig.GetVisualSizeTier(behaviorType);
            actorView.SetSprite(sprite);
            actorView.SetVisualCanvasHeight(ActorVisualSizeTierCatalog.GetCanvasHeightMeters(sizeTier));
            if (frameYawChanged || isWalking)
            {
                actorView.SetRotationY(frameYawDegrees);
            }

            actorView.SetFlip(false);
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
