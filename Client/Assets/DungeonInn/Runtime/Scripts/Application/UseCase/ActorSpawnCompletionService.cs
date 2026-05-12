using System;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.Profiles;
using DungeonInn.Domain.Actor;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class ActorSpawnCompletionService
    {
        readonly IActorProfileRegistry profileRegistry;
        readonly IEventPublisher eventBus;

        [Inject]
        public ActorSpawnCompletionService(
            IActorProfileRegistry profileRegistry,
            IEventPublisher eventBus)
        {
            this.profileRegistry = profileRegistry ?? throw new ArgumentNullException(nameof(profileRegistry));
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public void Complete(Actor actor, ActorArchetypeMaster archetypeMaster, string displayName)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            if (archetypeMaster == null)
            {
                throw new ArgumentNullException(nameof(archetypeMaster));
            }

            var resolvedDisplayName = string.IsNullOrWhiteSpace(displayName)
                ? archetypeMaster.Name
                : displayName;

            profileRegistry.Register(
                actor.Id,
                resolvedDisplayName,
                archetypeMaster.Id,
                archetypeMaster.SpeciesId,
                archetypeMaster.BehaviorType);
            eventBus.Publish(new ActorSpawned(actor.Id));
        }
    }
}
