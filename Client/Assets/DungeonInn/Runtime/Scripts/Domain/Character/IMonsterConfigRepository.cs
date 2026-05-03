using Cysharp.Threading.Tasks;

namespace DungeonInn.Domain.Character
{
    public interface IMonsterConfigRepository
    {
        UniTask<MonsterConfigData> LoadAsync();
    }
}
