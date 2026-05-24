using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DungeonInn.GameSession.Settings;
using DungeonInn.View.Scene.MainScene.World;
using LighthouseExtends.Addressable;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World.Settings
{
    public sealed class WorldMapViewSettingsRepository : IWorldMapViewSettingsRepository
    {
        const string Address = "Config/WorldGameSettings";

        readonly IAssetScope assetScope;

        WorldMapViewSettings worldMapViewSettings;
        bool loaded;

        [Inject]
        public WorldMapViewSettingsRepository(IAssetScope assetScope)
        {
            this.assetScope = assetScope ?? throw new ArgumentNullException(nameof(assetScope));
        }

        public async UniTask LoadAsync(CancellationToken cancellationToken)
        {
            if (loaded)
            {
                return;
            }

            var handle = await assetScope.LoadAsync<WorldGameSettingsSO>(Address, cancellationToken);
            var settingsSO = handle.Asset;
            worldMapViewSettings = settingsSO != null
                ? new WorldMapViewSettings(
                    settingsSO.MapChunkTileSize,
                    settingsSO.MapChunkBuildsPerFrame,
                    settingsSO.MapTileHeightMeters)
                : WorldMapViewSettings.CreateDefault();
            loaded = true;
        }

        public WorldMapViewSettings GetWorldMapViewSettings()
        {
            EnsureLoaded();
            return worldMapViewSettings;
        }

        void EnsureLoaded()
        {
            if (!loaded)
            {
                throw new InvalidOperationException("World map view settings are not loaded.");
            }
        }
    }
}
