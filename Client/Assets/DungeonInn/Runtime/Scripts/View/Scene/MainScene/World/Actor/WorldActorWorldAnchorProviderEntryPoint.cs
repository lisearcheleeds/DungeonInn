using System;
using DungeonInn.View.Scene.Bridge;
using VContainer;
using VContainer.Unity;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldActorWorldAnchorProviderEntryPoint : IStartable, IDisposable
    {
        readonly WorldActorWorldAnchorProvider provider;
        readonly IActorWorldAnchorProviderRegistry actorWorldAnchorProviderRegistry;
        readonly IWorldHudCameraProviderRegistry worldHudCameraProviderRegistry;

        [Inject]
        public WorldActorWorldAnchorProviderEntryPoint(
            WorldActorWorldAnchorProvider provider,
            IActorWorldAnchorProviderRegistry actorWorldAnchorProviderRegistry,
            IWorldHudCameraProviderRegistry worldHudCameraProviderRegistry)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
            this.actorWorldAnchorProviderRegistry =
                actorWorldAnchorProviderRegistry ?? throw new ArgumentNullException(nameof(actorWorldAnchorProviderRegistry));
            this.worldHudCameraProviderRegistry =
                worldHudCameraProviderRegistry ?? throw new ArgumentNullException(nameof(worldHudCameraProviderRegistry));
        }

        public void Start()
        {
            actorWorldAnchorProviderRegistry.Register(provider);
            worldHudCameraProviderRegistry.Register(provider);
        }

        public void Dispose()
        {
            actorWorldAnchorProviderRegistry.Unregister(provider);
            worldHudCameraProviderRegistry.Unregister(provider);
        }
    }
}
