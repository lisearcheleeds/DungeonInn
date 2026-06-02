using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD
{
    public sealed class GameHUDEntryPoint : IAsyncStartable, ITickable
    {
        readonly GameHUDViewFactory viewFactory;
        readonly WorldActorStatusPresenter worldActorStatusPresenter;
        readonly DamageNumberPresenter damageNumberPresenter;
        bool isInitialized;

        [Inject]
        public GameHUDEntryPoint(
            GameHUDViewFactory viewFactory,
            WorldActorStatusPresenter worldActorStatusPresenter,
            DamageNumberPresenter damageNumberPresenter)
        {
            this.viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
            this.worldActorStatusPresenter =
                worldActorStatusPresenter ?? throw new ArgumentNullException(nameof(worldActorStatusPresenter));
            this.damageNumberPresenter =
                damageNumberPresenter ?? throw new ArgumentNullException(nameof(damageNumberPresenter));
        }

        public async UniTask StartAsync(CancellationToken cancellation)
        {
            await viewFactory.LoadAsync(cancellation);
            damageNumberPresenter.Initialize();
            isInitialized = true;
        }

        public void Tick()
        {
            if (!isInitialized)
            {
                return;
            }

            worldActorStatusPresenter.UpdatePositions();
            damageNumberPresenter.UpdateAnimations(Time.unscaledDeltaTime);
        }
    }
}
