using System;
using System.Collections.Generic;
using DungeonInn.Domain.Map;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldAreaEffectViewPool : IDisposable
    {
        readonly Stack<AreaEffectView> inactiveViews = new();
        readonly Dictionary<Guid, AreaEffectView> activeViews = new();
        readonly List<AreaEffectView> createdViews = new();
        readonly AreaEffectPrefabSource prefabSource;
        readonly MapLayerViewRegistry layerViewRegistry;
        bool disposed;

        [Inject]
        public WorldAreaEffectViewPool(
            AreaEffectPrefabSource prefabSource,
            MapLayerViewRegistry layerViewRegistry)
        {
            this.prefabSource = prefabSource ?? throw new ArgumentNullException(nameof(prefabSource));
            this.layerViewRegistry = layerViewRegistry ?? throw new ArgumentNullException(nameof(layerViewRegistry));
        }

        public AreaEffectView Rent(Guid areaEffectId, MapLayerId layerId, string prefabAddress)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(WorldAreaEffectViewPool));
            }

            if (activeViews.TryGetValue(areaEffectId, out var existingView))
            {
                return existingView;
            }

            var areaEffectView = inactiveViews.Count == 0
                ? InstantiateView(prefabAddress)
                : inactiveViews.Pop();
#if DEBUG
            areaEffectView.gameObject.name = $"AreaEffect_{areaEffectId}";
#else
            areaEffectView.gameObject.name = "AreaEffect";
#endif
            foreach (var childTransform in areaEffectView.GetComponentsInChildren<Transform>(true))
            {
                childTransform.gameObject.layer = WorldRenderingLayer.Layer;
            }

            var actorRoot = layerViewRegistry.GetOrCreateActorRoot(layerId);
            areaEffectView.transform.SetParent(actorRoot, false);
            areaEffectView.gameObject.SetActive(true);
            areaEffectView.Reset();
            activeViews.Add(areaEffectId, areaEffectView);
            return areaEffectView;
        }

        public void Return(Guid areaEffectId)
        {
            if (!activeViews.TryGetValue(areaEffectId, out var areaEffectView))
            {
                return;
            }

            activeViews.Remove(areaEffectId);
            areaEffectView.gameObject.SetActive(false);
            areaEffectView.transform.SetParent(null, false);
            areaEffectView.Reset();

            if (disposed)
            {
                DestroyAreaEffectObject(areaEffectView.gameObject);
                return;
            }

            inactiveViews.Push(areaEffectView);
        }

        public void ForEach(Action<Guid, AreaEffectView> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            foreach (var (id, areaEffectView) in activeViews)
            {
                action(id, areaEffectView);
            }
        }

        public bool TryGetActive(Guid areaEffectId, out AreaEffectView view)
        {
            return activeViews.TryGetValue(areaEffectId, out view);
        }

        public void Dispose()
        {
            disposed = true;
            foreach (var areaEffectView in createdViews)
            {
                if (areaEffectView != null && areaEffectView.gameObject != null)
                {
                    DestroyAreaEffectObject(areaEffectView.gameObject);
                }
            }

            inactiveViews.Clear();
            activeViews.Clear();
            createdViews.Clear();
        }

        AreaEffectView InstantiateView(string prefabAddress)
        {
            var areaEffectView = UnityEngine.Object.Instantiate(prefabSource.GetPrefab(prefabAddress));
            areaEffectView.gameObject.layer = WorldRenderingLayer.Layer;
            createdViews.Add(areaEffectView);
            return areaEffectView;
        }

        static void DestroyAreaEffectObject(GameObject areaEffectObject)
        {
#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(areaEffectObject);
                return;
            }
#endif
            UnityEngine.Object.Destroy(areaEffectObject);
        }
    }
}
