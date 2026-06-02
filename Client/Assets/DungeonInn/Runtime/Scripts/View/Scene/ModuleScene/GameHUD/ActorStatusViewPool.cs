using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using Object = UnityEngine.Object;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD
{
    public sealed class ActorStatusViewPool : IDisposable
    {
        readonly Queue<ActorStatusView> inactiveViews = new();
        readonly Dictionary<Guid, ActorStatusView> activeViews = new();
        readonly List<ActorStatusView> createdViews = new();
        readonly GameHUDViewFactory viewFactory;
        readonly GameHUDModuleScene gameHUDModuleScene;

        bool disposed;

        [Inject]
        public ActorStatusViewPool(
            GameHUDViewFactory viewFactory,
            GameHUDModuleScene gameHUDModuleScene)
        {
            this.viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
            this.gameHUDModuleScene = gameHUDModuleScene ?? throw new ArgumentNullException(nameof(gameHUDModuleScene));
        }

        public ActorStatusView Rent(Guid actorId)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(ActorStatusViewPool));
            }

            if (activeViews.TryGetValue(actorId, out var existingView))
            {
                return existingView;
            }

            var view = inactiveViews.Count == 0
                ? InstantiateView()
                : inactiveViews.Dequeue();
#if DEBUG
            view.gameObject.name = $"ActorStatus_{actorId}";
#else
            view.gameObject.name = "ActorStatus";
#endif
            view.transform.SetParent(gameHUDModuleScene.HudRoot, false);
            view.gameObject.SetActive(true);
            view.ResetView();
            activeViews.Add(actorId, view);
            return view;
        }

        public void Return(Guid actorId)
        {
            if (!activeViews.TryGetValue(actorId, out var view))
            {
                return;
            }

            activeViews.Remove(actorId);
            view.gameObject.SetActive(false);
            view.ResetView();

            if (disposed)
            {
                DestroyObject(view.gameObject);
                return;
            }

            inactiveViews.Enqueue(view);
        }

        public void TryReturnIfActive(Guid actorId)
        {
            if (activeViews.ContainsKey(actorId))
            {
                Return(actorId);
            }
        }

        public bool TryGetActive(Guid actorId, out ActorStatusView view)
        {
            return activeViews.TryGetValue(actorId, out view);
        }

        public void Dispose()
        {
            disposed = true;
            foreach (var view in createdViews)
            {
                if (view != null && view.gameObject != null)
                {
                    DestroyObject(view.gameObject);
                }
            }

            inactiveViews.Clear();
            activeViews.Clear();
            createdViews.Clear();
        }

        ActorStatusView InstantiateView()
        {
            var prefab = viewFactory.ActorStatusViewPrefab;
            if (prefab == null)
            {
                throw new InvalidOperationException("[ActorStatusViewPool] ActorStatusView prefab is not loaded.");
            }

            var view = Object.Instantiate(prefab, gameHUDModuleScene.HudRoot);
            createdViews.Add(view);
            return view;
        }

        static void DestroyObject(GameObject target)
        {
#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                Object.DestroyImmediate(target);
                return;
            }
#endif
            Object.Destroy(target);
        }
    }
}
