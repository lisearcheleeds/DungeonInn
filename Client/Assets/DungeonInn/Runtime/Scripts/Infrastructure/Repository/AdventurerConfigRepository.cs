using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Character;
using LighthouseExtends.Addressable;
using VContainer;

namespace DungeonInn.Infrastructure.Repository
{
    public class AdventurerConfigRepository : IAdventurerConfigRepository
    {
        readonly IAssetManager assetManager;
        AdventurerConfigData cached;

        [Inject]
        public AdventurerConfigRepository(IAssetManager assetManager)
        {
            this.assetManager = assetManager;
        }

        public async UniTask<AdventurerConfigData> LoadAsync()
        {
            if (cached != null) return cached;
            using var scope = assetManager.CreateScope();
            var handle = await scope.LoadAsync<AdventurerConfigSO>("Config/AdventurerConfig");
            cached = handle.Asset.ToData();
            return cached;
        }
    }
}
