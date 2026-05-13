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
            foreach (var actor in gameWorldState.Actors)
            {
                activeActorIds.Add(actor.Id);
                var actorView = actorViewRegistry.GetOrCreateActorView(
                    actor,
                    actorSpriteVisualConfig.GetPlaceholderSprite(actor));
                actorViewRegistry.SetActorLayer(actorView, actor);
                actorView.ActorObject.transform.position = positionMapper.ToActorUnityPosition(actor.Position);
                actorView.ActorObject.transform.rotation = UnityEngine.Quaternion.Euler(0f, worldCameraController.CurrentYawDegrees, 0f);
                actorView.UpdateFacing(actor.Position);
                ApplyCameraRelativeFlip(actorView);
            }

            actorViewRegistry.RemoveMissingActorObjects(activeActorIds);
        }

        public void Dispose()
        {
        }

        void ApplyCameraRelativeFlip(WorldActorView actorView)
        {
            var yawRotation = UnityEngine.Quaternion.Euler(0f, worldCameraController.CurrentYawDegrees, 0f);
            var cameraRight = yawRotation * UnityEngine.Vector3.right;
            var facing = new UnityEngine.Vector3(actorView.Facing.x, 0f, actorView.Facing.y);
            actorView.SpriteRenderer.flipX = UnityEngine.Vector3.Dot(cameraRight, facing) < 0f;
        }
    }
}
