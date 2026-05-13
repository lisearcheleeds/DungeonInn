using System;
using System.Collections.Generic;
using DungeonInn.Application.GameLoop;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldActorPresenter : IDisposable
    {
        readonly IGameWorldStateReader gameWorldState;
        readonly LayerPositionViewMapper positionMapper;
        readonly WorldActorViewRegistry actorViewRegistry;
        readonly ActorSpriteVisualConfig actorSpriteVisualConfig;
        readonly HashSet<Guid> activeActorIds = new();

        public WorldActorPresenter(
            IGameWorldStateReader gameWorldState,
            LayerPositionViewMapper positionMapper,
            WorldActorViewRegistry actorViewRegistry,
            ActorSpriteVisualConfig actorSpriteVisualConfig)
        {
            this.gameWorldState = gameWorldState ?? throw new ArgumentNullException(nameof(gameWorldState));
            this.positionMapper = positionMapper ?? throw new ArgumentNullException(nameof(positionMapper));
            this.actorViewRegistry = actorViewRegistry ?? throw new ArgumentNullException(nameof(actorViewRegistry));
            this.actorSpriteVisualConfig = actorSpriteVisualConfig ?? throw new ArgumentNullException(nameof(actorSpriteVisualConfig));
        }

        public void UpdateVisuals()
        {
            activeActorIds.Clear();
            foreach (var actor in gameWorldState.Actors)
            {
                activeActorIds.Add(actor.Id);
                var actorObject = actorViewRegistry.GetOrCreateActorObject(
                    actor,
                    actorSpriteVisualConfig.GetDebugMaterial(actor));
                actorViewRegistry.SetActorLayer(actorObject, actor);
                actorObject.transform.position = positionMapper.ToActorUnityPosition(actor.Position);
            }

            actorViewRegistry.RemoveMissingActorObjects(activeActorIds);
        }

        public void Dispose()
        {
        }
    }
}
