using VContainer;
using VContainer.Unity;

namespace DungeonInn.Runtime.Scripts.View.Scene.MainScene.World
{
    public class WorldLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<WorldScene>();
        }
    }
}
