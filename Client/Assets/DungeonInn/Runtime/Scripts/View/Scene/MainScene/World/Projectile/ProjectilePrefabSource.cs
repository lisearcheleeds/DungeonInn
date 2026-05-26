using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class ProjectilePrefabSource : IInitializable, IDisposable
    {
        readonly WorldAddressableViewFactory viewFactory;
        Texture2D placeholderTexture;
        ProjectileView fallbackPrefab;

        [Inject]
        public ProjectilePrefabSource(WorldAddressableViewFactory viewFactory)
        {
            this.viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
        }

        public ProjectileView GetPrefab(string prefabAddress)
        {
            return viewFactory.GetProjectilePrefab(prefabAddress) ?? fallbackPrefab;
        }

        public void Initialize()
        {
            var projectileObject = new GameObject("ProjectilePrefab_Default");
            projectileObject.layer = WorldRenderingLayer.Layer;
            var spriteRenderer = projectileObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = PlaceholderAssetFactory.CreateProjectilePlaceholder(
                Color.yellow,
                out placeholderTexture);
            fallbackPrefab = projectileObject.AddComponent<ProjectileView>();
            projectileObject.SetActive(false);
        }

        public void Dispose()
        {
            if (placeholderTexture != null)
            {
                DestroyObject(placeholderTexture);
            }

            if (fallbackPrefab != null)
            {
                DestroyObject(fallbackPrefab.gameObject);
            }
        }

        static void DestroyObject(UnityEngine.Object target)
        {
#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(target);
                return;
            }
#endif
            UnityEngine.Object.Destroy(target);
        }
    }
}
