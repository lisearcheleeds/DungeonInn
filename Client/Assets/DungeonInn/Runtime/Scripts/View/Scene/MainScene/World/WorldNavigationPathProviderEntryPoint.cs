using System;
using DungeonInn.View.Scene;
using DungeonInn.View.Scene.Bridge;
using VContainer;
using VContainer.Unity;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldNavigationPathProviderEntryPoint : IStartable, IDisposable
    {
        readonly UnityNavMeshPathProvider provider;
        readonly INavigationPathProviderRegistry registry;

        [Inject]
        public WorldNavigationPathProviderEntryPoint(
            UnityNavMeshPathProvider provider,
            INavigationPathProviderRegistry registry)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public void Start()
        {
            registry.Register(provider);
        }

        public void Dispose()
        {
            registry.Unregister(provider);
        }
    }
}
