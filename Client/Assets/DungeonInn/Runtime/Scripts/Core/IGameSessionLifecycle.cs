using Lighthouse.Scene;
using VContainer.Unity;

namespace DungeonInn.Core
{
    public interface IGameSessionLifecycle
    {
        LifetimeScope ActiveScope { get; }
        void BeginSession();
        void EndSession();
        bool IsSessionScene(MainSceneId sceneId);
    }
}
