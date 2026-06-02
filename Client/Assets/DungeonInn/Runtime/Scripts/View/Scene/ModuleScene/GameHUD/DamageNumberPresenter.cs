using System;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.View.Scene.Bridge;
using R3;
using VContainer;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD
{
    public sealed class DamageNumberPresenter : IDisposable
    {
        readonly IEventSubscriber eventSubscriber;
        readonly IActorWorldAnchorProvider actorWorldAnchorProvider;
        readonly IActiveLayerProvider activeLayerProvider;
        readonly IDamageNumberViewSpawner viewSpawner;
        DisposableBag bag;
        bool initialized;

        [Inject]
        public DamageNumberPresenter(
            IEventSubscriber eventSubscriber,
            IActorWorldAnchorProvider actorWorldAnchorProvider,
            IActiveLayerProvider activeLayerProvider,
            IDamageNumberViewSpawner viewSpawner)
        {
            this.eventSubscriber = eventSubscriber ?? throw new ArgumentNullException(nameof(eventSubscriber));
            this.actorWorldAnchorProvider =
                actorWorldAnchorProvider ?? throw new ArgumentNullException(nameof(actorWorldAnchorProvider));
            this.activeLayerProvider = activeLayerProvider ?? throw new ArgumentNullException(nameof(activeLayerProvider));
            this.viewSpawner = viewSpawner ?? throw new ArgumentNullException(nameof(viewSpawner));
        }

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            eventSubscriber.OnEvent<CombatAttackOccurred>()
                .Subscribe(OnCombatAttackOccurred)
                .AddTo(ref bag);
            initialized = true;
        }

        public void UpdateAnimations(float deltaSeconds)
        {
            viewSpawner.Tick(deltaSeconds);
        }

        public void Dispose()
        {
            bag.Dispose();
        }

        void OnCombatAttackOccurred(CombatAttackOccurred gameEvent)
        {
            if (gameEvent.Damage <= 0)
            {
                return;
            }

            if (!actorWorldAnchorProvider.TryGetWorldAnchor(
                    gameEvent.TargetActorId,
                    out var worldPosition,
                    out var layerId))
            {
                return;
            }

            var activeLayerId = activeLayerProvider.ActiveLayerId;
            if (!activeLayerId.HasValue ||
                layerId.Value != activeLayerId.Value)
            {
                return;
            }

            viewSpawner.Spawn(gameEvent.Damage, worldPosition);
        }
    }
}
