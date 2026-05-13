using System;
using System.Collections.Generic;
using DungeonInn.Application.GameLoop;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldActorPresenter : IDisposable
    {
        readonly IGameWorldStateReader gameWorldState;
        readonly LayerPositionViewMapper positionMapper;
        readonly WorldActorViewRegistry actorViewRegistry;
        readonly ActorSpriteVisualConfig actorSpriteVisualConfig;
        readonly WorldCameraController worldCameraController;
        readonly HashSet<Guid> activeActorIds = new();
        bool hasLastCameraYawDegrees;
        float lastCameraYawDegrees;

        [Inject]
        public WorldActorPresenter(
            IGameWorldStateReader gameWorldState,
            LayerPositionViewMapper positionMapper,
            WorldActorViewRegistry actorViewRegistry,
            ActorSpriteVisualConfig actorSpriteVisualConfig,
            WorldCameraController worldCameraController)
        {
            this.gameWorldState = gameWorldState ?? throw new ArgumentNullException(nameof(gameWorldState));
            this.positionMapper = positionMapper ?? throw new ArgumentNullException(nameof(positionMapper));
            this.actorViewRegistry = actorViewRegistry ?? throw new ArgumentNullException(nameof(actorViewRegistry));
            this.actorSpriteVisualConfig = actorSpriteVisualConfig ?? throw new ArgumentNullException(nameof(actorSpriteVisualConfig));
            this.worldCameraController = worldCameraController ?? throw new ArgumentNullException(nameof(worldCameraController));
        }

        public void UpdateVisuals()
        {
            activeActorIds.Clear();
            var cameraYawDegrees = worldCameraController.CurrentYawDegrees;
            var cameraYawChanged = !hasLastCameraYawDegrees ||
                !UnityEngine.Mathf.Approximately(lastCameraYawDegrees, cameraYawDegrees);

            foreach (var actor in gameWorldState.Actors)
            {
                activeActorIds.Add(actor.Id);
                var actorView = actorViewRegistry.GetOrCreateActorView(
                    actor,
                    actorSpriteVisualConfig.GetPlaceholderSprite(actor),
                    out var created);

                var positionChanged = created ||
                    !actorView.HasLastPosition ||
                    !IsSamePosition(actorView.LastPosition, actor.Position);
                var facingChanged = false;
                if (positionChanged)
                {
                    if (!created && !actorView.LastPosition.LayerId.Equals(actor.Position.LayerId))
                    {
                        actorViewRegistry.SetActorLayer(actorView, actor);
                    }

                    actorView.ActorObject.transform.localPosition =
                        positionMapper.ToActorLayerLocalPosition(actor.Position);
                    facingChanged = actorView.UpdateFacing(actor.Position);
                }

                if (created || positionChanged || cameraYawChanged)
                {
                    actorView.ActorObject.transform.rotation = UnityEngine.Quaternion.Euler(0f, cameraYawDegrees, 0f);
                }

                if (created || facingChanged || cameraYawChanged)
                {
                    ApplyCameraRelativeFlip(actorView, cameraYawDegrees);
                }
            }

            if (actorViewRegistry.Count != activeActorIds.Count)
            {
                actorViewRegistry.RemoveMissingActorObjects(activeActorIds);
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
