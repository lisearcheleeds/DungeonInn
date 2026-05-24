using Cysharp.Threading.Tasks;

namespace DungeonInn.Core
{
    public interface IEntrySceneTransitionService
    {
        UniTask TransitionToEntrySceneAsync();
    }
}
