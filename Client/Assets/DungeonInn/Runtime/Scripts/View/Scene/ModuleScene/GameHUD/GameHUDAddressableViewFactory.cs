using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using LighthouseExtends.Addressable;
using VContainer;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD
{
    public sealed class GameHUDAddressableViewFactory : IDisposable
    {
        const string ActorStatusViewAddress = "GameHUD/UI/ActorStatusView";
        const string ActorDetailPopupAddress = "GameHUD/UI/ActorDetailPopup";
        const string PlayerEventLogViewAddress = "GameHUD/UI/PlayerEventLogView";
        const string WorldHudViewAddress = "GameHUD/UI/WorldHudView";
        const string InnStatusPanelViewAddress = "GameHUD/UI/InnStatusPanelView";
        const string MinimapViewAddress = "GameHUD/UI/MinimapView";

        readonly IAssetManager assetManager;

        IAssetScope assetScope;
        PlayerEventLogView playerEventLogViewPrefab;
        WorldHudView worldHudViewPrefab;
        InnStatusPanelView innStatusPanelViewPrefab;
        MinimapView minimapViewPrefab;

        [Inject]
        public GameHUDAddressableViewFactory(IAssetManager assetManager)
        {
            this.assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));
        }

        public ActorStatusView ActorStatusViewPrefab { get; private set; }
        public ActorDetailPopup ActorDetailPopupPrefab { get; private set; }

        public async UniTask LoadAsync(CancellationToken ct)
        {
            assetScope = assetManager.CreateScope();
            ActorStatusViewPrefab = await LoadComponentPrefabAsync<ActorStatusView>(ActorStatusViewAddress, ct);
            ActorDetailPopupPrefab = await LoadComponentPrefabAsync<ActorDetailPopup>(ActorDetailPopupAddress, ct);
            playerEventLogViewPrefab = await LoadComponentPrefabAsync<PlayerEventLogView>(PlayerEventLogViewAddress, ct);
            worldHudViewPrefab = await LoadComponentPrefabAsync<WorldHudView>(WorldHudViewAddress, ct);
            innStatusPanelViewPrefab = await LoadComponentPrefabAsync<InnStatusPanelView>(InnStatusPanelViewAddress, ct);
            minimapViewPrefab = await LoadComponentPrefabAsync<MinimapView>(MinimapViewAddress, ct);
        }

        public ActorDetailPopup CreateActorDetailPopup(Transform parent)
        {
            if (ActorDetailPopupPrefab == null || parent == null)
            {
                return null;
            }

            return InstantiateHudView(ActorDetailPopupPrefab, parent);
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
                    $"[GameHUDAddressableViewFactory] Failed to load prefab. Address={address} Error={exception.Message}");
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
                $"[GameHUDAddressableViewFactory] Loaded prefab does not have {typeof(TView).Name}. Address={address}");
            return null;
        }

        static TView InstantiateHudView<TView>(TView prefab, Transform parent)
            where TView : Component
        {
            var view = UnityEngine.Object.Instantiate(prefab, parent);
            GameHUDRenderingLayer.ApplyToHierarchy(view.gameObject);
            return view;
        }
    }
}
