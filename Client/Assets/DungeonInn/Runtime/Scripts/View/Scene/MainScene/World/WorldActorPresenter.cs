using System;
using System.Collections.Generic;
using DungeonInn.Application.GameLoop;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldActorPresenter : IDisposable
    {
        readonly IActorViewDataProvider viewDataProvider;
        readonly LayerPositionViewMapper positionMapper;
        readonly WorldActorViewRegistry actorViewRegistry;
        readonly ActorSpriteVisualConfig actorSpriteVisualConfig;
        readonly WorldCameraController worldCameraController;
        bool hasLastCameraYawDegrees;
        float lastCameraYawDegrees;

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
        }

        public void UpdateVisuals()
        {
            var cameraYawDegrees = worldCameraController.CurrentYawDegrees;
            var cameraYawChanged = !hasLastCameraYawDegrees ||
                !UnityEngine.Mathf.Approximately(lastCameraYawDegrees, cameraYawDegrees);
            var changes = viewDataProvider.ConsumeChanges();

            foreach (var actorId in changes.RemovedActorIds)
            {
                actorViewRegistry.RemoveActorObject(actorId);
            }

            foreach (var actor in changes.ChangedActors)
            {
                var actorView = actorViewRegistry.GetOrCreateActorView(
                    actor.ActorId,
                    actor.Position,
                    actorSpriteVisualConfig.GetPlaceholderSprite(actor.BehaviorType),
                    out var created);

                var positionChanged = created ||
                    !actorView.HasLastPosition ||
                    !IsSamePosition(actorView.LastPosition, actor.Position);
                var facingChanged = false;
                if (positionChanged)
                {
                    if (!created && !actorView.LastPosition.LayerId.Equals(actor.Position.LayerId))
                    {
                        actorViewRegistry.SetActorLayer(actorView, actor.Position);
                    }

                    actorView.ActorObject.transform.localPosition =
                        positionMapper.ToActorLayerLocalPosition(actor.Position);
                    facingChanged = actorView.UpdateFacing(actor.Position);
                }

                if (!cameraYawChanged && (created || positionChanged))
                {
                    actorView.ActorObject.transform.rotation = UnityEngine.Quaternion.Euler(0f, cameraYawDegrees, 0f);
                }

                if (!cameraYawChanged && (created || facingChanged))
                {
                    ApplyCameraRelativeFlip(actorView, cameraYawDegrees);
                }
            }

            if (cameraYawChanged)
            {
                actorViewRegistry.ForEachActorView(actorView =>
                {
                    actorView.ActorObject.transform.rotation = UnityEngine.Quaternion.Euler(0f, cameraYawDegrees, 0f);
                    ApplyCameraRelativeFlip(actorView, cameraYawDegrees);
                });
            }

            lastCameraYawDegrees = cameraYawDegrees;
            hasLastCameraYawDegrees = true;
        }

        public void Dispose()
        {
        }

        static bool IsSamePosition(DungeonInn.Domain.Map.LayerPosition first, DungeonInn.Domain.Map.LayerPosition second)
        {
            return first.LayerId.Equals(second.LayerId) &&
                UnityEngine.Mathf.Approximately(first.X, second.X) &&
                UnityEngine.Mathf.Approximately(first.Z, second.Z);
        }

        void ApplyCameraRelativeFlip(WorldActorView actorView, float cameraYawDegrees)
        {
            var yawRotation = UnityEngine.Quaternion.Euler(0f, cameraYawDegrees, 0f);
            var cameraRight = yawRotation * UnityEngine.Vector3.right;
            var facing = new UnityEngine.Vector3(actorView.Facing.x, 0f, actorView.Facing.y);
            actorView.SpriteRenderer.flipX = UnityEngine.Vector3.Dot(cameraRight, facing) < 0f;
        }
    }
}
