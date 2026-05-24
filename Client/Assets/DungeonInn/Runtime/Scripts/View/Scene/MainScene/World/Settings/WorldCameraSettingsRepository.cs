using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DungeonInn.View.Scene.MainScene.World;
using LighthouseExtends.Addressable;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World.Settings
{
    public sealed class WorldCameraSettingsRepository : IWorldCameraSettingsRepository
    {
        const string Address = "Config/WorldCameraSettings";

        readonly IAssetScope assetScope;
        WorldCameraSettings settings = WorldCameraSettingsSO.CreateFallbackSettings();
        bool loaded;

        [Inject]
        public WorldCameraSettingsRepository(IAssetScope assetScope)
        {
            this.assetScope = assetScope ?? throw new ArgumentNullException(nameof(assetScope));
        }

        public async UniTask LoadAsync(CancellationToken cancellationToken)
        {
            if (loaded)
            {
                return;
            }

            var handle = await assetScope.LoadAsync<WorldCameraSettingsSO>(Address, cancellationToken);
            settings = handle.Asset != null
                ? handle.Asset.ToSettings()
                : WorldCameraSettingsSO.CreateFallbackSettings();
            loaded = true;
        }

        public WorldCameraSettings Get()
        {
            if (!loaded)
            {
                throw new InvalidOperationException("World camera settings are not loaded.");
            }

            return settings;
        }
    }
}
