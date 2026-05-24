using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DungeonInn.View.Scene.MainScene.World;
using LighthouseExtends.Addressable;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World.Settings
{
    public sealed class LayerPositionViewSettingsRepository : ILayerPositionViewSettingsRepository
    {
        const string Address = "Config/LayerPositionViewSettings";

        readonly IAssetScope assetScope;
        LayerPositionViewSettings settings;
        bool loaded;

        [Inject]
        public LayerPositionViewSettingsRepository(IAssetScope assetScope)
        {
            this.assetScope = assetScope ?? throw new ArgumentNullException(nameof(assetScope));
        }

        public async UniTask LoadAsync(CancellationToken cancellationToken)
        {
            if (loaded)
            {
                return;
            }

            var handle = await assetScope.LoadAsync<LayerPositionViewSettingsSO>(Address, cancellationToken);
            settings = handle.Asset != null
                ? handle.Asset.ToSettings()
                : LayerPositionViewSettingsSO.CreateFallbackSettings();
            loaded = true;
        }

        public LayerPositionViewSettings Get()
        {
            if (!loaded)
            {
                throw new InvalidOperationException("Layer position view settings are not loaded.");
            }

            return settings;
        }
    }
}
