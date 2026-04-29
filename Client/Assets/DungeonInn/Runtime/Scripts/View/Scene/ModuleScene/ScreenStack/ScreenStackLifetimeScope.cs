using LighthouseExtends.ScreenStack;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using YourProduct;

namespace DungeonInn.Runtime.Scripts.View.Scene.ModuleScene.ScreenStack
{
    public sealed class ScreenStackLifetimeScope : ScreenStackLifetimeScopeBase
    {
        [SerializeField] ScreenStackBackgroundInputBlocker screenStackBackgroundInputBlockerPrefab;

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);

            builder.Register<ScreenStackEntityFactory>(Lifetime.Singleton).AsImplementedInterfaces();

            builder.RegisterComponentInNewPrefab(screenStackBackgroundInputBlockerPrefab, Lifetime.Singleton).AsImplementedInterfaces();
        }
    }
}
