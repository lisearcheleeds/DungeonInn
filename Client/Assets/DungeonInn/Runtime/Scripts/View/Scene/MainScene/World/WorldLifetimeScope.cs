using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldLifetimeScope : LifetimeScope
    {
        [SerializeField] WorldScene worldScene;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(worldScene);
            builder.Register<WorldPresenter>(Lifetime.Singleton).AsImplementedInterfaces();
        }
    }
}
