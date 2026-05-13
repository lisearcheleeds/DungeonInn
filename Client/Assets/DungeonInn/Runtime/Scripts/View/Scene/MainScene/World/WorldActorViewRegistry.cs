using System;
using System.Collections.Generic;
using DungeonInn.Domain.Map;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldActorViewRegistry : IDisposable
    {
        readonly MapLayerViewRegistry layerViewRegistry;
        readonly Dictionary<Guid, WorldActorView> actorViews = new();

        public int Count => actorViews.Count;

        [Inject]
        public WorldActorViewRegistry(MapLayerViewRegistry layerViewRegistry)
        {
            this.layerViewRegistry = layerViewRegistry ?? throw new ArgumentNullException(nameof(layerViewRegistry));
        }

        public WorldActorView GetOrCreateActorView(Guid actorId, LayerPosition position, Sprite sprite, out bool created)
        {
            if (actorViews.TryGetValue(actorId, out var actorView))
            {
                created = false;
                if (actorView.SpriteRenderer.sprite != sprite)
                {
                    actorView.SpriteRenderer.sprite = sprite;
                }

                return actorView;
            }

            var actorObject = new GameObject($"Actor_{actorId}");
            actorObject.name = $"Actor_{actorId}";
            actorObject.layer = WorldRenderingLayer.Layer;
            var spriteRenderer = actorObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            actorView = new WorldActorView(actorObject, spriteRenderer);
            actorViews.Add(actorId, actorView);
            SetActorLayer(actorView, position);
            created = true;
            return actorView;
        }

        public void SetActorLayer(WorldActorView actorView, LayerPosition position)
        {
            var actorRoot = layerViewRegistry.GetOrCreateActorRoot(position.LayerId);
            if (actorView.ActorObject.transform.parent != actorRoot)
            {
                actorView.ActorObject.transform.SetParent(actorRoot, false);
            }
        }

        public void RemoveActorObject(Guid actorId)
        {
            if (!actorViews.TryGetValue(actorId, out var actorView))
            {
                return;
            }

            UnityEngine.Object.Destroy(actorView.ActorObject);
            actorViews.Remove(actorId);
        }

        public void ForEachActorView(Action<WorldActorView> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            foreach (var actorView in actorViews.Values)
            {
                action(actorView);
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
