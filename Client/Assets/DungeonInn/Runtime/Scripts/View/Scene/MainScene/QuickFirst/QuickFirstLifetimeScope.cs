using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DungeonInn.Runtime.Scripts.View.Scene.MainScene.QuickFirst
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
