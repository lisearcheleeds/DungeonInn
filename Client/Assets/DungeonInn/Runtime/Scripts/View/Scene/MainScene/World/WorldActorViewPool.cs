using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldActorViewPool : IDisposable
    {
        readonly Stack<ActorView> inactiveViews = new();
        readonly List<ActorView> createdViews = new();
        readonly ActorPrefabSource prefabSource;
        bool disposed;

        [Inject]
        public WorldActorViewPool(ActorPrefabSource prefabSource)
        {
            this.prefabSource = prefabSource ?? throw new ArgumentNullException(nameof(prefabSource));
        }

        public int InactiveCount => inactiveViews.Count;
        public int CreatedCount => createdViews.Count;

        public ActorView Rent(Guid actorId)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(WorldActorViewPool));
            }

            var actorView = inactiveViews.Count == 0
                ? InstantiateView()
                : inactiveViews.Pop();
#if DEBUG
            actorView.gameObject.name = $"Actor_{actorId}";
#else
            actorView.gameObject.name = "Actor";
#endif
            actorView.gameObject.layer = WorldRenderingLayer.Layer;
            actorView.gameObject.SetActive(true);
            actorView.Reset();
            return actorView;
        }

        public void Return(ActorView actorView)
        {
            if (actorView == null || actorView.gameObject == null)
            {
                return;
            }

            actorView.gameObject.SetActive(false);
            actorView.transform.SetParent(null, false);
            actorView.Reset();

            if (disposed)
            {
                DestroyActorObject(actorView.gameObject);
                return;
            }

            inactiveViews.Push(actorView);
        }

        public void Dispose()
        {
            disposed = true;
            foreach (var actorView in createdViews)
            {
                if (actorView != null && actorView.gameObject != null)
                {
                    DestroyActorObject(actorView.gameObject);
                }
            }

            inactiveViews.Clear();
            createdViews.Clear();
        }

        ActorView InstantiateView()
        {
            var actorView = UnityEngine.Object.Instantiate(prefabSource.Prefab);
            actorView.gameObject.layer = WorldRenderingLayer.Layer;
            createdViews.Add(actorView);
            return actorView;
        }

        static void DestroyActorObject(GameObject actorObject)
        {
#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(actorObject);
                return;
            }
#endif
            UnityEngine.Object.Destroy(actorObject);
        }
    }
}
