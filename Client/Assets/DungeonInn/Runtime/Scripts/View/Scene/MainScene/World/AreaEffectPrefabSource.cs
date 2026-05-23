using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class AreaEffectPrefabSource : IInitializable, IDisposable
    {
        readonly WorldAddressableViewFactory viewFactory;
        Texture2D placeholderTexture;
        AreaEffectView fallbackPrefab;

        [Inject]
        public AreaEffectPrefabSource(WorldAddressableViewFactory viewFactory)
        {
            this.viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
        }

        public AreaEffectView GetPrefab(string prefabAddress)
        {
            return viewFactory.GetAreaEffectPrefab(prefabAddress) ?? fallbackPrefab;
        }

        public void Initialize()
        {
            var areaEffectObject = new GameObject("AreaEffectPrefab_Default");
            areaEffectObject.layer = WorldRenderingLayer.Layer;
            var spriteRenderer = areaEffectObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = PlaceholderAssetFactory.CreateAreaEffectPlaceholder(
                Color.magenta,
                out placeholderTexture);
            fallbackPrefab = areaEffectObject.AddComponent<AreaEffectView>();
            areaEffectObject.SetActive(false);
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
