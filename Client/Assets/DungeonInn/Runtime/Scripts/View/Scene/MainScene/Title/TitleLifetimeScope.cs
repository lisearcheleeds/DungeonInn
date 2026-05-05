using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DungeonInn.View.Scene.MainScene.Title
{
    public sealed class TitleLifetimeScope : LifetimeScope
    {
        [SerializeField] TitleScene titleScene;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(titleScene);
            builder.Register<TitlePresenter>(Lifetime.Scoped).AsImplementedInterfaces();
        }
    }
}
