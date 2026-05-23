using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Actor;
using LighthouseExtends.Addressable;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class VisualConfigLoader : IDisposable
    {
        readonly IAssetManager assetManager;
        readonly MapMaterialSetSO mapMaterialSetSO;
        readonly Dictionary<TileVisualKind, Material> loadedMaterials = new();

        IAssetScope assetScope;

        [Inject]
        public VisualConfigLoader(
            IAssetManager assetManager,
            VisualConfigSettings settings)
        {
            this.assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            mapMaterialSetSO = settings.MapMaterialSetSO;
        }

        public async UniTask LoadAsync(CancellationToken ct)
        {
            assetScope = assetManager.CreateScope();

            if (mapMaterialSetSO != null)
            {
                await LoadMaterialsAsync(ct);
            }

        }

        public Material GetMaterial(TileVisualKind kind)
        {
            loadedMaterials.TryGetValue(kind, out var material);
            return material;
        }

        public void Dispose()
        {
            assetScope?.Dispose();
        }

        async UniTask LoadMaterialsAsync(CancellationToken ct)
        {
            foreach (var entry in mapMaterialSetSO.Entries)
            {
                if (string.IsNullOrWhiteSpace(entry.MaterialAddress))
                {
                    continue;
                }

                try
                {
                    var handle = await assetScope.LoadAsync<Material>(entry.MaterialAddress, ct);
                    loadedMaterials[entry.Kind] = handle.Asset;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    Debug.LogWarning(
                        $"[VisualConfigLoader] Failed to load material. Address={entry.MaterialAddress} Error={exception.Message}");
                }
            }
        }

    }
}
