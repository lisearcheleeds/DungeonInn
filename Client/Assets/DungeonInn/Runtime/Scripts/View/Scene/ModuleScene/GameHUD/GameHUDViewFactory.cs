using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LighthouseExtends.Addressable;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD
{
    public sealed class GameHUDViewFactory : IDisposable
    {
        const string ActorStatusViewAddress = "GameHUD/ActorStatusView";
        const string DamageNumberViewAddress = "GameHUD/DamageNumberView";

        readonly IAssetManager assetManager;

        IAssetScope assetScope;

        [Inject]
        public GameHUDViewFactory(IAssetManager assetManager)
        {
            this.assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));
        }

        public ActorStatusView ActorStatusViewPrefab { get; private set; }
        public DamageNumberView DamageNumberViewPrefab { get; private set; }

        public async UniTask LoadAsync(CancellationToken ct)
        {
            assetScope = assetManager.CreateScope();
            ActorStatusViewPrefab = await LoadComponentPrefabAsync<ActorStatusView>(ActorStatusViewAddress, ct);
            DamageNumberViewPrefab = await LoadComponentPrefabAsync<DamageNumberView>(DamageNumberViewAddress, ct);
            if (ActorStatusViewPrefab == null || DamageNumberViewPrefab == null)
            {
                throw new InvalidOperationException(
                    "[GameHUDViewFactory] Required GameHUD prefabs are missing. " +
                    $"ActorStatusView={ActorStatusViewPrefab != null} DamageNumberView={DamageNumberViewPrefab != null}");
            }
        }

        public void Dispose()
        {
            assetScope?.Dispose();
        }

        async UniTask<TView> LoadComponentPrefabAsync<TView>(string address, CancellationToken ct)
            where TView : Component
        {
            try
            {
                var handle = await assetScope.LoadAsync<GameObject>(address, ct);
                if (handle.Asset != null && handle.Asset.TryGetComponent<TView>(out var view))
                {
                    return view;
                }

                Debug.LogWarning(
                    $"[GameHUDViewFactory] Loaded prefab does not have {typeof(TView).Name}. Address={address}");
                return null;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"[GameHUDViewFactory] Failed to load prefab. Address={address} Error={exception.Message}");
                return null;
            }
        }
    }
}
