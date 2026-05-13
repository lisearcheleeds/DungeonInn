using System;
using System.Collections.Generic;
using R3;
using VContainer;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;

namespace DungeonInn.Application.Actors.Lifecycle
{
    public sealed class AdventurerRecoveryStateService : IDisposable
    {
        readonly Dictionary<Guid, float> accumulatedHp = new();
        DisposableBag bag;

        [Inject]
        public AdventurerRecoveryStateService(IEventSubscriber eventSubscriber)
        {
            if (eventSubscriber == null)
            {
                throw new ArgumentNullException(nameof(eventSubscriber));
            }

            eventSubscriber.OnEvent<ActorDefeated>()
                .Subscribe(gameEvent => { Remove(gameEvent.ActorId); })
                .AddTo(ref bag);
            eventSubscriber.OnEvent<ActorDeparted>()
                .Subscribe(gameEvent => { Remove(gameEvent.ActorId); })
                .AddTo(ref bag);
        }

        public float GetAccumulatedHp(Guid actorId)
        {
            return accumulatedHp.TryGetValue(actorId, out var accumulated)
                ? accumulated
                : 0f;
        }

        public void SetAccumulatedHp(Guid actorId, float accumulated)
        {
            accumulatedHp[actorId] = accumulated;
        }

        public void Remove(Guid actorId)
        {
            accumulatedHp.Remove(actorId);
        }

        public void Dispose()
        {
            bag.Dispose();
        }
    }
}
