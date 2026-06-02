using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using Object = UnityEngine.Object;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD
{
    public sealed class DamageNumberViewPool : IDamageNumberViewSpawner, IDisposable
    {
        readonly Queue<DamageNumberView> inactiveViews = new();
        readonly List<DamageNumberView> activeViews = new();
        readonly List<DamageNumberView> createdViews = new();
        readonly GameHUDViewFactory viewFactory;
        readonly GameHUDModuleScene gameHUDModuleScene;
        readonly DungeonInn.View.Scene.Bridge.IWorldHudCameraProvider worldHudCameraProvider;
        bool disposed;

        [Inject]
        public DamageNumberViewPool(
            GameHUDViewFactory viewFactory,
            GameHUDModuleScene gameHUDModuleScene,
            DungeonInn.View.Scene.Bridge.IWorldHudCameraProvider worldHudCameraProvider)
        {
            this.viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
            this.gameHUDModuleScene = gameHUDModuleScene ?? throw new ArgumentNullException(nameof(gameHUDModuleScene));
            this.worldHudCameraProvider =
                worldHudCameraProvider ?? throw new ArgumentNullException(nameof(worldHudCameraProvider));
        }

        public void Spawn(int damage, Vector3 worldPosition)
        {
            if (disposed)
            {
                return;
            }

            var view = inactiveViews.Count == 0 ? InstantiateView() : inactiveViews.Dequeue();
            view.SetBillboardRotation(worldHudCameraProvider.CameraRotation);
            view.SetScreenScale(worldHudCameraProvider.WorldUnitsPerPixel);
            view.Show(damage, worldPosition);
            activeViews.Add(view);
        }

        public void Tick(float deltaSeconds)
        {
            for (var index = activeViews.Count - 1; 0 <= index; index--)
            {
                var view = activeViews[index];
                if (view != null)
                {
                    view.SetBillboardRotation(worldHudCameraProvider.CameraRotation);
                    view.SetScreenScale(worldHudCameraProvider.WorldUnitsPerPixel);
                }

                if (view != null && view.Tick(deltaSeconds))
                {
                    continue;
                }

                activeViews.RemoveAt(index);
                Return(view);
            }
        }

        public void Dispose()
        {
            disposed = true;
            for (var index = 0; index < createdViews.Count; index++)
            {
                var view = createdViews[index];
                if (view != null && view.gameObject != null)
                {
                    DestroyObject(view.gameObject);
                }
            }

            inactiveViews.Clear();
            activeViews.Clear();
            createdViews.Clear();
        }

        DamageNumberView InstantiateView()
        {
            var prefab = viewFactory.DamageNumberViewPrefab;
            if (prefab == null)
            {
                throw new InvalidOperationException("[DamageNumberViewPool] DamageNumberView prefab is not loaded.");
            }

            var view = Object.Instantiate(prefab, gameHUDModuleScene.HudRoot);
            createdViews.Add(view);
            return view;
        }

        void Return(DamageNumberView view)
        {
            if (view == null)
            {
                return;
            }

            view.ResetView();
            view.gameObject.SetActive(false);
            if (disposed)
            {
                DestroyObject(view.gameObject);
                return;
            }

            inactiveViews.Enqueue(view);
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
