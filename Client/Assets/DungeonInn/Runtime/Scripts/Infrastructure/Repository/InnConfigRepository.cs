using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Inn;
using LighthouseExtends.Addressable;
using VContainer;

namespace DungeonInn.Infrastructure.Repository
{
    public class InnConfigRepository : IInnConfigRepository
    {
        readonly IAssetManager assetManager;
        InnConfigData cached;

        [Inject]
        public InnConfigRepository(IAssetManager assetManager)
        {
            this.assetManager = assetManager;
        }

        public async UniTask<InnConfigData> LoadAsync()
        {
            if (cached != null) return cached;
            using var scope = assetManager.CreateScope();
            var handle = await scope.LoadAsync<InnConfigSO>("Config/InnConfig");
            cached = handle.Asset.ToData();
            return cached;
        }
    }
}
