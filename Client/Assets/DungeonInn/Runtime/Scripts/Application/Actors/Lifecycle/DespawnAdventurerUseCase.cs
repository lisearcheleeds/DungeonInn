using DungeonInn.Application.World;
using System;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using VContainer;

namespace DungeonInn.Application.Actors.Lifecycle
{
    public sealed class DespawnAdventurerUseCase
    {
        readonly IEventPublisher eventPublisher;

        [Inject]
        public DespawnAdventurerUseCase(IEventPublisher eventPublisher)
        {
            this.eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        }

        public void Execute(IGameWorldState worldState, Actor actor, int waitedDays)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            if (worldState.Guild.HasActiveInnReservation(actor.Id))
            {
                return;
            }

            if (actor.Behavior is not AdventurerBehavior)
            {
                return;
            }

            if (!worldState.RemoveActor(actor.Id))
            {
                return;
            }

            worldState.Guild.RemoveQueuedInnReservation(actor.Id);
            eventPublisher.Publish(new ActorDeparted(actor.Id, waitedDays));
        }
    }
}
