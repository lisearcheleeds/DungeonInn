using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.World;
using DungeonInn.Master;
using DungeonInn.View.Scene.ModuleScene.WorldUI;
using LighthouseExtends.Addressable;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldAddressableViewFactory : IDisposable
    {
        const string ActorStatusViewAddress = "World/UI/ActorStatusView";
        const string ActorDetailPopupAddress = "World/UI/ActorDetailPopup";
        const string PlayerEventLogViewAddress = "World/UI/PlayerEventLogView";

        readonly IAssetManager assetManager;
        readonly IMasterRepository masterRepository;
        readonly Dictionary<string, GameObject> propPrefabs = new();
        readonly Dictionary<string, ProjectileView> projectilePrefabs = new();
        readonly Dictionary<string, AreaEffectView> areaEffectPrefabs = new();

        IAssetScope assetScope;

        [Inject]
        public WorldAddressableViewFactory(
            IAssetManager assetManager,
            IMasterRepository masterRepository)
        {
            this.assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
        }

        public ActorStatusView ActorStatusViewPrefab { get; private set; }
        public ActorDetailPopup ActorDetailPopupPrefab { get; private set; }
        public PlayerEventLogView PlayerEventLogViewPrefab { get; private set; }

        public async UniTask LoadAsync(CancellationToken ct)
        {
            assetScope = assetManager.CreateScope();
            ActorStatusViewPrefab = await LoadComponentPrefabAsync<ActorStatusView>(ActorStatusViewAddress, ct);
            ActorDetailPopupPrefab = await LoadComponentPrefabAsync<ActorDetailPopup>(ActorDetailPopupAddress, ct);
            PlayerEventLogViewPrefab = await LoadComponentPrefabAsync<PlayerEventLogView>(PlayerEventLogViewAddress, ct);
            await LoadCombatPrefabsAsync(ct);
            await LoadPropPrefabsAsync(ct);
        }

        public GameObject GetPropPrefab(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return null;
            }

            propPrefabs.TryGetValue(address, out var prefab);
            return prefab;
        }

        public ProjectileView GetProjectilePrefab(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return null;
            }

            projectilePrefabs.TryGetValue(address, out var prefab);
            return prefab;
        }

        public AreaEffectView GetAreaEffectPrefab(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return null;
            }

            areaEffectPrefabs.TryGetValue(address, out var prefab);
            return prefab;
        }

        public ActorDetailPopup CreateActorDetailPopup(Transform parent)
        {
            if (ActorDetailPopupPrefab == null || parent == null)
            {
                return null;
            }

            return UnityEngine.Object.Instantiate(ActorDetailPopupPrefab, parent);
        }

        public void Dispose()
        {
            assetScope?.Dispose();
        }

        async UniTask LoadCombatPrefabsAsync(CancellationToken ct)
        {
            foreach (var master in masterRepository.WeaponTypeCombatMasters.Values)
            {
                if (!string.IsNullOrWhiteSpace(master.ProjectilePrefabAddress) &&
                    !projectilePrefabs.ContainsKey(master.ProjectilePrefabAddress))
                {
                    projectilePrefabs[master.ProjectilePrefabAddress] =
                        await LoadComponentPrefabAsync<ProjectileView>(master.ProjectilePrefabAddress, ct);
                }

                if (!string.IsNullOrWhiteSpace(master.AreaEffectPrefabAddress) &&
                    !areaEffectPrefabs.ContainsKey(master.AreaEffectPrefabAddress))
                {
                    areaEffectPrefabs[master.AreaEffectPrefabAddress] =
                        await LoadComponentPrefabAsync<AreaEffectView>(master.AreaEffectPrefabAddress, ct);
                }
            }
        }

        async UniTask LoadPropPrefabsAsync(CancellationToken ct)
        {
            foreach (var master in masterRepository.EnvironmentPropVisualMasters.Values)
            {
                if (string.IsNullOrWhiteSpace(master.PrefabAddress) ||
                    propPrefabs.ContainsKey(master.PrefabAddress))
                {
                    continue;
                }

                propPrefabs[master.PrefabAddress] = await LoadPrefabAsync(master.PrefabAddress, ct);
            }
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
                    $"[WorldAddressableViewFactory] Failed to load prefab. Address={address} Error={exception.Message}");
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
                $"[WorldAddressableViewFactory] Loaded prefab does not have {typeof(TView).Name}. Address={address}");
            return null;
        }
    }
}
