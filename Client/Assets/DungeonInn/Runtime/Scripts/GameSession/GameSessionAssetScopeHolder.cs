using System;
using LighthouseExtends.Addressable;
using VContainer;

namespace DungeonInn.GameSession
{
    public sealed class GameSessionAssetScopeHolder : IDisposable
    {
        [Inject]
        public GameSessionAssetScopeHolder(IAssetManager assetManager)
        {
            if (assetManager == null)
            {
                throw new ArgumentNullException(nameof(assetManager));
            }

            AssetScope = assetManager.CreateScope();
        }

        public IAssetScope AssetScope { get; }

        public void Dispose()
        {
            AssetScope.Dispose();
        }
    }
}
