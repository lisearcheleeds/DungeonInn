using Cysharp.Threading.Tasks;
using Lighthouse.Scene;

namespace DungeonInn.Core
{
    public interface IProductSceneManager
    {
        bool IsTransition { get; }

        UniTask TransitionScene(
            TransitionDataBase nextTransitionData,
            TransitionType transitionType = TransitionType.Auto,
            MainSceneId backMainSceneId = default);

        UniTask BackScene(TransitionType transitionType = TransitionType.Auto);

        UniTask PreReboot();
    }
}
