using VContainer;
using VContainer.Unity;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD
{
    public sealed class GameHUDLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<GameHUDModuleScene>();
            builder.RegisterComponentInHierarchy<ActorEffectIconSpriteCatalog>();
            builder.RegisterEntryPoint<GameHUDEntryPoint>();
            builder.Register<GameHUDViewFactory>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
            builder.Register<ActorStatusViewPool>(Lifetime.Scoped).AsSelf();
            builder.Register<DamageNumberViewPool>(Lifetime.Scoped)
                .As<IDamageNumberViewSpawner>()
                .AsSelf();
            builder.Register<WorldActorStatusPresenter>(Lifetime.Scoped).AsSelf();
            builder.Register<DamageNumberPresenter>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
        }
    }
}
