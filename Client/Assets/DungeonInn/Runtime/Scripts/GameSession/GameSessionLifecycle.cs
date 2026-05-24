using System;
using DungeonInn.Core;
using DungeonInn.LighthouseGenerated;
using Lighthouse.Scene;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DungeonInn.GameSession
{
    public sealed class GameSessionLifecycle : IGameSessionLifecycle, IRebootCleanupTarget, IDisposable
    {
        readonly ProductLifetimeScope productLifetimeScope;
        readonly IRebootCleanupRegistry rebootCleanupRegistry;
        LifetimeScope gameSessionLifetimeScope;

        public LifetimeScope ActiveScope => gameSessionLifetimeScope;

        [Inject]
        public GameSessionLifecycle(
            ProductLifetimeScope productLifetimeScope,
            IRebootCleanupRegistry rebootCleanupRegistry)
        {
            this.productLifetimeScope = productLifetimeScope
                ?? throw new ArgumentNullException(nameof(productLifetimeScope));
            this.rebootCleanupRegistry =
                rebootCleanupRegistry ?? throw new ArgumentNullException(nameof(rebootCleanupRegistry));
        }

        public void BeginSession()
        {
            if (gameSessionLifetimeScope != null)
            {
                return;
            }

            gameSessionLifetimeScope = productLifetimeScope.CreateChild<GameSessionLifetimeScope>();
            rebootCleanupRegistry.Register(this);
        }

        public void EndSession()
        {
            if (gameSessionLifetimeScope == null)
            {
                return;
            }

            UnityEngine.Object.Destroy(gameSessionLifetimeScope.gameObject);
            gameSessionLifetimeScope = null;
            rebootCleanupRegistry.Unregister(this);
        }

        public bool IsSessionScene(MainSceneId sceneId)
        {
            return sceneId == DungeonInnMainSceneId.World;
        }

        public void Dispose()
        {
            EndSession();
        }

        public void CleanupBeforeReboot()
        {
            EndSession();
        }
    }
}
