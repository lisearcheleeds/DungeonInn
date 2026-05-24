using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DungeonInn.View.Scene.MainScene.Title
{
    public sealed class TitleLifetimeScope : LifetimeScope
    {
        [SerializeField] TitleScene titleScene;
        [SerializeField] TitleView titleView;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(titleScene);
            builder.RegisterComponent(titleView);
            builder.Register<TitlePresenter>(Lifetime.Scoped).AsImplementedInterfaces();
        }
    }
}
