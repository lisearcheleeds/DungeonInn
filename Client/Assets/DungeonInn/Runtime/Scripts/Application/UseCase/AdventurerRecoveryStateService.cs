using System;
using System.Collections.Generic;
using R3;
using VContainer;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;

namespace DungeonInn.Application.UseCase
{
    public sealed class AdventurerRecoveryStateService : IDisposable
    {
        readonly Dictionary<Guid, float> accumulatedHp = new();
        readonly IDisposable deathSubscription;

        [Inject]
        public AdventurerRecoveryStateService(IEventSubscriber eventSubscriber)
        {
            if (eventSubscriber == null)
            {
                throw new ArgumentNullException(nameof(eventSubscriber));
            }

            deathSubscription = eventSubscriber.OnEvent<ActorDefeated>()
                .Subscribe(gameEvent => { Remove(gameEvent.ActorId); });
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
            deathSubscription.Dispose();
        }
    }
}
