using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using LighthouseExtends.Addressable;
using VContainer;

namespace DungeonInn.View.Scene.ModuleScene.GameUI
{
    public sealed class GameUIAddressableViewFactory : IDisposable
    {
        const string SelectedActorInspectorViewAddress = "GameUI/SelectedActorInspectorView";
        const string PlayerEventLogViewAddress = "GameUI/PlayerEventLogView";
        const string WorldHudViewAddress = "GameUI/WorldHudView";
        const string InnStatusPanelViewAddress = "GameUI/InnStatusPanelView";
        const string MinimapViewAddress = "GameUI/MinimapView";

        readonly IAssetManager assetManager;

        IAssetScope assetScope;
        PlayerEventLogView playerEventLogViewPrefab;
        WorldHudView worldHudViewPrefab;
        InnStatusPanelView innStatusPanelViewPrefab;
        MinimapView minimapViewPrefab;

        [Inject]
        public GameUIAddressableViewFactory(IAssetManager assetManager)
        {
            this.assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));
        }

        public SelectedActorInspectorView SelectedActorInspectorViewPrefab { get; private set; }

        public async UniTask LoadAsync(CancellationToken ct)
        {
            assetScope = assetManager.CreateScope();
            SelectedActorInspectorViewPrefab =
                await LoadComponentPrefabAsync<SelectedActorInspectorView>(SelectedActorInspectorViewAddress, ct);
            playerEventLogViewPrefab = await LoadComponentPrefabAsync<PlayerEventLogView>(PlayerEventLogViewAddress, ct);
            worldHudViewPrefab = await LoadComponentPrefabAsync<WorldHudView>(WorldHudViewAddress, ct);
            innStatusPanelViewPrefab = await LoadComponentPrefabAsync<InnStatusPanelView>(InnStatusPanelViewAddress, ct);
            minimapViewPrefab = await LoadComponentPrefabAsync<MinimapView>(MinimapViewAddress, ct);
        }

        public SelectedActorInspectorView CreateSelectedActorInspectorView(Transform parent)
        {
            if (SelectedActorInspectorViewPrefab == null || parent == null)
            {
                return null;
            }

            return InstantiateHudView(SelectedActorInspectorViewPrefab, parent);
        }

        public PlayerEventLogView CreatePlayerEventLogView(Transform parent)
        {
            if (playerEventLogViewPrefab == null || parent == null)
            {
                return null;
            }

            return InstantiateHudView(playerEventLogViewPrefab, parent);
        }

        public WorldHudView CreateWorldHudView(Transform parent)
        {
            if (worldHudViewPrefab == null || parent == null)
            {
                return null;
            }

            return InstantiateHudView(worldHudViewPrefab, parent);
        }

        public InnStatusPanelView CreateInnStatusPanelView(Transform parent)
        {
            if (innStatusPanelViewPrefab == null || parent == null)
            {
                return null;
            }

            return InstantiateHudView(innStatusPanelViewPrefab, parent);
        }

        public MinimapView CreateMinimapView(Transform parent)
        {
            if (minimapViewPrefab == null || parent == null)
            {
                return null;
            }

            return InstantiateHudView(minimapViewPrefab, parent);
        }

        public void Dispose()
        {
            assetScope?.Dispose();
        }

        async UniTask<GameObject> LoadPrefabAsync(string address, CancellationToken ct)
        {
            try
            {
                var handle = await assetScope.LoadAsync<GameObject>(address, ct);
                return handle.Asset;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"[GameUIAddressableViewFactory] Failed to load prefab. Address={address} Error={exception.Message}");
                return null;
            }
        }

        async UniTask<TView> LoadComponentPrefabAsync<TView>(string address, CancellationToken ct)
            where TView : Component
        {
            var prefab = await LoadPrefabAsync(address, ct);
            if (prefab == null)
            {
                return null;
            }

            if (prefab.TryGetComponent<TView>(out var view))
            {
                return view;
            }

            Debug.LogWarning(
                $"[GameUIAddressableViewFactory] Loaded prefab does not have {typeof(TView).Name}. Address={address}");
            return null;
        }

        static TView InstantiateHudView<TView>(TView prefab, Transform parent)
            where TView : Component
        {
            var view = UnityEngine.Object.Instantiate(prefab, parent);
            GameUIRenderingLayer.ApplyToHierarchy(view.gameObject);
            return view;
        }
    }
}

