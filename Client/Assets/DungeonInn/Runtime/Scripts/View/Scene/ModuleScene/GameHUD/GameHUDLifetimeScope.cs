using VContainer;
using VContainer.Unity;
using DungeonInn.View.Scene.ModuleScene.GameHUD.ScreenStack;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD
{
    public sealed class GameHUDLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<GameHUDModuleScene>();
            builder.RegisterEntryPoint<GameHUDEntryPoint>();
            builder.Register<GameHUDAddressableViewFactory>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();

            builder.Register<ActorHUDViewPool>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
            builder.Register<WorldActorStatusPresenter>(Lifetime.Scoped).AsSelf();
            builder.Register<ActorDetailPopupPresenter>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
            builder.Register<MinimapPresenter>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
            builder.Register<GameHudScreenStackViewDataFactory>(Lifetime.Scoped);
            builder.Register<GameHudWindowOpenService>(Lifetime.Scoped).AsImplementedInterfaces();
            builder.Register<WorldHudPresenter>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
            builder.Register<InnStatusPanelPresenter>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
            builder.Register<PlayerGameEventLogPresenter>(Lifetime.Scoped).AsSelf();
        }
    }
}
