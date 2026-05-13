using System;
using System.Collections.Generic;
using DungeonInn.Domain.Actor;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldActorViewRegistry : IDisposable
    {
        readonly MapLayerViewRegistry layerViewRegistry;
        readonly Dictionary<Guid, WorldActorView> actorViews = new();
        readonly List<Guid> removeActorIds = new();

        [Inject]
        public WorldActorViewRegistry(MapLayerViewRegistry layerViewRegistry)
        {
            this.layerViewRegistry = layerViewRegistry ?? throw new ArgumentNullException(nameof(layerViewRegistry));
        }

        public WorldActorView GetOrCreateActorView(Actor actor, Sprite sprite)
        {
            if (actorViews.TryGetValue(actor.Id, out var actorView))
            {
                actorView.SpriteRenderer.sprite = sprite;
                return actorView;
            }

            var actorObject = new GameObject($"Actor_{actor.Id}");
            actorObject.name = $"Actor_{actor.Id}";
            actorObject.layer = WorldRenderingLayer.Layer;
            var spriteRenderer = actorObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            actorView = new WorldActorView(actorObject, spriteRenderer);
            actorViews.Add(actor.Id, actorView);
            SetActorLayer(actorView, actor);
            return actorView;
        }

        public void SetActorLayer(WorldActorView actorView, Actor actor)
        {
            var actorRoot = layerViewRegistry.GetOrCreateActorRoot(actor.Position.LayerId);
            if (actorView.ActorObject.transform.parent != actorRoot)
            {
                actorView.ActorObject.transform.SetParent(actorRoot, false);
            }
        }

        public void RemoveMissingActorObjects(HashSet<Guid> activeActorIds)
        {
            removeActorIds.Clear();
            foreach (var pair in actorViews)
            {
                if (!activeActorIds.Contains(pair.Key))
                {
                    removeActorIds.Add(pair.Key);
                }
            }

            foreach (var actorId in removeActorIds)
            {
                UnityEngine.Object.Destroy(actorViews[actorId].ActorObject);
                actorViews.Remove(actorId);
            }
        }

        public void Dispose()
        {
            foreach (var actorView in actorViews.Values)
            {
                if (actorView.ActorObject != null)
                {
                    UnityEngine.Object.Destroy(actorView.ActorObject);
                }
            }

            actorViews.Clear();
        }
    }
}
