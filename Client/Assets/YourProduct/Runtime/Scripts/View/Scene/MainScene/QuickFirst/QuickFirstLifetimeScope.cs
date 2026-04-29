using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace YourProduct.View.Scene.MainScene.QuickFirst
{
    public class FirstSceneLifetimeScope : LifetimeScope
    {
        [SerializeField] FirstSceneScene quickFirstScene;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(quickFirstScene);

            builder.Register<FirstScenePresenter>(Lifetime.Singleton).AsImplementedInterfaces();
        }
    }
}
