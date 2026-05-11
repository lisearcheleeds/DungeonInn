using System;
using System.Collections.Generic;
using R3;
using VContainer;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.UseCase
{
    public sealed class AdventurerExplorationStateService : IDisposable
    {
        readonly Dictionary<Guid, LayerPosition> destinations = new();
        readonly IDisposable deathSubscription;

        [Inject]
        public AdventurerExplorationStateService(IEventSubscriber eventSubscriber)
        {
            if (eventSubscriber == null)
            {
                throw new ArgumentNullException(nameof(eventSubscriber));
            }

            deathSubscription = eventSubscriber.OnEvent<ActorDefeated>()
                .Subscribe(gameEvent => { RemoveDestination(gameEvent.ActorId); });
        }

        public bool TryGetDestination(Guid actorId, out LayerPosition destination)
        {
            return destinations.TryGetValue(actorId, out destination);
        }

        public void SetDestination(Guid actorId, LayerPosition destination)
        {
            destinations[actorId] = destination;
        }

        public void RemoveDestination(Guid actorId)
        {
            destinations.Remove(actorId);
        }

        public void Dispose()
        {
            deathSubscription.Dispose();
        }
    }
}
