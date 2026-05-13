using System;
using System.Collections.Generic;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldActorPresenter : IDisposable
    {
        readonly IGameWorldStateReader gameWorldState;
        readonly LayerPositionViewMapper positionMapper;
        readonly WorldActorViewRegistry actorViewRegistry;
        readonly Material adventurerMaterial;
        readonly Material monsterMaterial;
        readonly Material otherActorMaterial;

        public WorldActorPresenter(
            IGameWorldStateReader gameWorldState,
            LayerPositionViewMapper positionMapper,
            WorldActorViewRegistry actorViewRegistry)
        {
            this.gameWorldState = gameWorldState ?? throw new ArgumentNullException(nameof(gameWorldState));
            this.positionMapper = positionMapper ?? throw new ArgumentNullException(nameof(positionMapper));
            this.actorViewRegistry = actorViewRegistry ?? throw new ArgumentNullException(nameof(actorViewRegistry));
            adventurerMaterial = WorldDebugMaterialFactory.Create(new Color(0.1f, 0.45f, 1f, 1f));
            monsterMaterial = WorldDebugMaterialFactory.Create(new Color(0.9f, 0.15f, 0.1f, 1f));
            otherActorMaterial = WorldDebugMaterialFactory.Create(new Color(1f, 0.85f, 0.1f, 1f));
        }

        public void UpdateVisuals()
        {
            var activeActorIds = new HashSet<Guid>();
            foreach (var actor in gameWorldState.Actors)
            {
                activeActorIds.Add(actor.Id);
                var actorObject = actorViewRegistry.GetOrCreateActorObject(actor, ResolveActorMaterial(actor));
                actorObject.transform.position = positionMapper.ToActorUnityPosition(actor.Position);
            }

            actorViewRegistry.RemoveMissingActorObjects(activeActorIds);
        }

        public void Dispose()
        {
            WorldDebugMaterialFactory.Dispose(adventurerMaterial);
            WorldDebugMaterialFactory.Dispose(monsterMaterial);
            WorldDebugMaterialFactory.Dispose(otherActorMaterial);
        }

        Material ResolveActorMaterial(Actor actor)
        {
            if (actor.Behavior is AdventurerBehavior)
            {
                return adventurerMaterial;
            }

            if (actor.Behavior is MonsterBehavior)
            {
                return monsterMaterial;
            }

            return otherActorMaterial;
        }
    }
}
