using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Character;
using LighthouseExtends.Addressable;
using VContainer;

namespace DungeonInn.Infrastructure.Repository
{
    public class MonsterConfigRepository : IMonsterConfigRepository
    {
        readonly IAssetManager assetManager;
        MonsterConfigData cached;

        [Inject]
        public MonsterConfigRepository(IAssetManager assetManager)
        {
            this.assetManager = assetManager;
        }

        public async UniTask<MonsterConfigData> LoadAsync()
        {
            if (cached != null) return cached;
            using var scope = assetManager.CreateScope();
            var handle = await scope.LoadAsync<MonsterConfigSO>("Config/MonsterConfig");
            cached = handle.Asset.ToData();
            return cached;
        }
    }
}
