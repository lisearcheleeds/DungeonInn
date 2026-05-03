using Cysharp.Threading.Tasks;

namespace DungeonInn.Domain.Dungeon
{
    public interface IDungeonConfigRepository
    {
        UniTask<DungeonConfigData> LoadAsync();
    }
}
