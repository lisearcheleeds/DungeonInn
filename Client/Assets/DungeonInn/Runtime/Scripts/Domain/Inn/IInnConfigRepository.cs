using Cysharp.Threading.Tasks;

namespace DungeonInn.Domain.Inn
{
    public interface IInnConfigRepository
    {
        UniTask<InnConfigData> LoadAsync();
    }
}
