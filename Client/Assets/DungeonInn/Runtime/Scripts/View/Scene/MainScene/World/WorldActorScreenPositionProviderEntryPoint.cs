using System;
using DungeonInn.View.Scene;
using DungeonInn.View.Scene.Bridge;
using VContainer;
using VContainer.Unity;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldActorScreenPositionProviderEntryPoint : IStartable, IDisposable
    {
        readonly WorldActorScreenPositionProvider provider;
        readonly IActorScreenPositionProviderRegistry registry;

        [Inject]
        public WorldActorScreenPositionProviderEntryPoint(
            WorldActorScreenPositionProvider provider,
            IActorScreenPositionProviderRegistry registry)
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
