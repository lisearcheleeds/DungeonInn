using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD
{
    public sealed class ActorHUDViewPool : IInitializable, IDisposable
    {
        readonly Queue<ActorStatusView> inactiveViews = new();
        readonly Dictionary<Guid, ActorStatusView> activeViews = new();
        readonly List<ActorStatusView> createdViews = new();
        readonly GameHUDAddressableViewFactory viewFactory;
        readonly GameHUDModuleScene gameHUDModuleScene;

        bool disposed;

        [Inject]
        public ActorHUDViewPool(
            GameHUDAddressableViewFactory viewFactory,
            GameHUDModuleScene gameHUDModuleScene)
        {
            this.viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
            this.gameHUDModuleScene = gameHUDModuleScene ?? throw new ArgumentNullException(nameof(gameHUDModuleScene));
        }

        public void Initialize()
        {
        }

        public ActorStatusView Rent(Guid actorId)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(ActorHUDViewPool));
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
            view.transform.SetParent(gameHUDModuleScene.HUDCanvas.transform, false);
            view.gameObject.SetActive(true);
            view.Reset();
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
            view.Reset();

            if (disposed)
            {
                DestroyObject(view.gameObject);
                return;
            }

            inactiveViews.Enqueue(view);
        }

        public void TryReturnIfActive(Guid actorId)
        {
            if (activeViews.TryGetValue(actorId, out _))
            {
                Return(actorId);
            }
        }

        public void ForEach(Action<Guid, ActorStatusView> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            foreach (var (actorId, view) in activeViews)
            {
                action(actorId, view);
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
            var view = prefab != null
                ? Object.Instantiate(prefab, gameHUDModuleScene.HUDCanvas.transform)
                : CreateFallbackView();
            GameHUDRenderingLayer.ApplyToHierarchy(view.gameObject);
            createdViews.Add(view);
            return view;
        }

        ActorStatusView CreateFallbackView()
        {
            var viewObject = new GameObject("ActorStatusView_Fallback", typeof(RectTransform));
            viewObject.transform.SetParent(gameHUDModuleScene.HUDCanvas.transform, false);
            return viewObject.AddComponent<ActorStatusView>();
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
