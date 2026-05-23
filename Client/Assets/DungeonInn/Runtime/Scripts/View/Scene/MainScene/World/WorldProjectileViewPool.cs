using System;
using System.Collections.Generic;
using DungeonInn.Domain.Map;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldProjectileViewPool : IDisposable
    {
        readonly Stack<ProjectileView> inactiveViews = new();
        readonly Dictionary<Guid, ProjectileView> activeViews = new();
        readonly List<ProjectileView> createdViews = new();
        readonly ProjectilePrefabSource prefabSource;
        readonly MapLayerViewRegistry layerViewRegistry;
        bool disposed;

        [Inject]
        public WorldProjectileViewPool(
            ProjectilePrefabSource prefabSource,
            MapLayerViewRegistry layerViewRegistry)
        {
            this.prefabSource = prefabSource ?? throw new ArgumentNullException(nameof(prefabSource));
            this.layerViewRegistry = layerViewRegistry ?? throw new ArgumentNullException(nameof(layerViewRegistry));
        }

        public ProjectileView Rent(Guid projectileId, MapLayerId layerId, string prefabAddress)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(WorldProjectileViewPool));
            }

            if (activeViews.TryGetValue(projectileId, out var existingView))
            {
                return existingView;
            }

            var projectileView = inactiveViews.Count == 0
                ? InstantiateView(prefabAddress)
                : inactiveViews.Pop();
#if DEBUG
            projectileView.gameObject.name = $"Projectile_{projectileId}";
#else
            projectileView.gameObject.name = "Projectile";
#endif
            foreach (var childTransform in projectileView.GetComponentsInChildren<Transform>(true))
            {
                childTransform.gameObject.layer = WorldRenderingLayer.Layer;
            }

            var actorRoot = layerViewRegistry.GetOrCreateActorRoot(layerId);
            projectileView.transform.SetParent(actorRoot, false);
            projectileView.gameObject.SetActive(true);
            projectileView.Reset();
            activeViews.Add(projectileId, projectileView);
            return projectileView;
        }

        public void Return(Guid projectileId)
        {
            if (!activeViews.TryGetValue(projectileId, out var projectileView))
            {
                return;
            }

            activeViews.Remove(projectileId);
            projectileView.gameObject.SetActive(false);
            projectileView.transform.SetParent(null, false);
            projectileView.Reset();

            if (disposed)
            {
                DestroyProjectileObject(projectileView.gameObject);
                return;
            }

            inactiveViews.Push(projectileView);
        }

        public void ForEach(Action<Guid, ProjectileView> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            foreach (var (id, projectileView) in activeViews)
            {
                action(id, projectileView);
            }
        }

        public void Dispose()
        {
            disposed = true;
            foreach (var projectileView in createdViews)
            {
                if (projectileView != null && projectileView.gameObject != null)
                {
                    DestroyProjectileObject(projectileView.gameObject);
                }
            }

            inactiveViews.Clear();
            activeViews.Clear();
            createdViews.Clear();
        }

        ProjectileView InstantiateView(string prefabAddress)
        {
            var projectileView = UnityEngine.Object.Instantiate(prefabSource.GetPrefab(prefabAddress));
            projectileView.gameObject.layer = WorldRenderingLayer.Layer;
            createdViews.Add(projectileView);
            return projectileView;
        }

        static void DestroyProjectileObject(GameObject projectileObject)
        {
#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(projectileObject);
                return;
            }
#endif
            UnityEngine.Object.Destroy(projectileObject);
        }
    }
}
