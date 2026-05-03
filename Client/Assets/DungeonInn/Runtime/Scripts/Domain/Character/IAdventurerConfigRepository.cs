using Cysharp.Threading.Tasks;

namespace DungeonInn.Domain.Character
{
    public interface IAdventurerConfigRepository
    {
        UniTask<AdventurerConfigData> LoadAsync();
    }
}
