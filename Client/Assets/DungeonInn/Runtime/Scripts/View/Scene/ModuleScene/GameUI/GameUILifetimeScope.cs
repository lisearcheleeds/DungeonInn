using VContainer;
using VContainer.Unity;
using DungeonInn.View.Scene.ModuleScene.GameUI.ScreenStack;

namespace DungeonInn.View.Scene.ModuleScene.GameUI
{
    public sealed class GameUILifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<GameUIModuleScene>();
            builder.RegisterEntryPoint<GameUIEntryPoint>();
            builder.Register<GameUIAddressableViewFactory>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
            builder.Register<SelectedActorInspectorPresenter>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
            builder.Register<MinimapPresenter>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
            builder.Register<GameUIScreenStackViewDataFactory>(Lifetime.Scoped);
            builder.Register<GameUIWindowOpenService>(Lifetime.Scoped).AsImplementedInterfaces();
            builder.Register<WorldHudPresenter>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
            builder.Register<InnStatusPanelPresenter>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
            builder.Register<PlayerGameEventLogPresenter>(Lifetime.Scoped).AsSelf();
        }
    }
}



