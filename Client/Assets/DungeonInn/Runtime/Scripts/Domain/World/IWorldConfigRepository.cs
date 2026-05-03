using Cysharp.Threading.Tasks;

namespace DungeonInn.Domain.World
{
    public interface IWorldConfigRepository
    {
        UniTask<WorldConfigData> LoadAsync();
    }
}
