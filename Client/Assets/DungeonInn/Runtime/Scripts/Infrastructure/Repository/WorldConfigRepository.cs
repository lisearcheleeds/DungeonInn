using Cysharp.Threading.Tasks;
using DungeonInn.Domain.World;
using LighthouseExtends.Addressable;
using VContainer;

namespace DungeonInn.Infrastructure.Repository
{
    public class WorldConfigRepository : IWorldConfigRepository
    {
        readonly IAssetManager assetManager;
        WorldConfigData cached;

        [Inject]
        public WorldConfigRepository(IAssetManager assetManager)
        {
            this.assetManager = assetManager;
        }

        public async UniTask<WorldConfigData> LoadAsync()
        {
            if (cached != null) return cached;
            using var scope = assetManager.CreateScope();
            var handle = await scope.LoadAsync<WorldConfigSO>("Config/WorldConfig");
            cached = handle.Asset.ToData();
            return cached;
        }
    }
}
