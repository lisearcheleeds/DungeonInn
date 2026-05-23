using VContainer;
using VContainer.Unity;

namespace DungeonInn.View.Scene.ModuleScene.WorldUI
{
    public sealed class WorldUILifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<WorldUIModuleScene>();
        }
    }
}
