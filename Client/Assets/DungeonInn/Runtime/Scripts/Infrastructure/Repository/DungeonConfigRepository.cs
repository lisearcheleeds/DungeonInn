using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Dungeon;
using LighthouseExtends.Addressable;
using VContainer;

namespace DungeonInn.Infrastructure.Repository
{
    public class DungeonConfigRepository : IDungeonConfigRepository
    {
        readonly IAssetManager assetManager;
        DungeonConfigData cached;

        [Inject]
        public DungeonConfigRepository(IAssetManager assetManager)
        {
            this.assetManager = assetManager;
        }

        public async UniTask<DungeonConfigData> LoadAsync()
        {
            if (cached != null) return cached;
            using var scope = assetManager.CreateScope();
            var handle = await scope.LoadAsync<DungeonConfigSO>("Config/DungeonConfig");
            cached = handle.Asset.ToData();
            return cached;
        }
    }
}
