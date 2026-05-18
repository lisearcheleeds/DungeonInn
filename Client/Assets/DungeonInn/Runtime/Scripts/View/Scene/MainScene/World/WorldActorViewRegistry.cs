using System;
using System.Collections.Generic;
using DungeonInn.Domain.Map;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldActorViewRegistry : IDisposable
    {
        readonly WorldActorViewPool actorViewPool;
        readonly MapLayerViewRegistry layerViewRegistry;
        readonly Dictionary<Guid, ActorView> actorViews = new();

        public int Count => actorViews.Count;

        [Inject]
        public WorldActorViewRegistry(
            WorldActorViewPool actorViewPool,
            MapLayerViewRegistry layerViewRegistry)
        {
            this.actorViewPool = actorViewPool ?? throw new ArgumentNullException(nameof(actorViewPool));
            this.layerViewRegistry = layerViewRegistry ?? throw new ArgumentNullException(nameof(layerViewRegistry));
        }

        public ActorView GetOrCreateActorView(Guid actorId, LayerPosition position, out bool created)
        {
            if (actorViews.TryGetValue(actorId, out var actorView))
            {
                created = false;

                return actorView;
            }

            actorView = actorViewPool.Rent(actorId);
            actorViews.Add(actorId, actorView);
            SetActorLayer(actorView, position);
            created = true;
            return actorView;
        }

        public void SetActorLayer(ActorView actorView, LayerPosition position)
        {
            var actorRoot = layerViewRegistry.GetOrCreateActorRoot(position.LayerId);
            if (actorView.transform.parent != actorRoot)
            {
                actorView.transform.SetParent(actorRoot, false);
            }
        }

        public void RemoveActorObject(Guid actorId)
        {
            if (!actorViews.TryGetValue(actorId, out var actorView))
            {
                return;
            }

            actorViews.Remove(actorId);
            actorViewPool.Return(actorView);
        }

        public void ForEachActorView(Action<Guid, ActorView> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            foreach (var (id, actorView) in actorViews)
            {
                action(id, actorView);
            }
        }

        public void Dispose()
        {
            foreach (var actorView in actorViews.Values)
            {
                actorViewPool.Return(actorView);
            }

            actorViews.Clear();
        }
    }
}
