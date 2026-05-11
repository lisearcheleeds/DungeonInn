using System;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class DespawnAdventurerUseCase
    {
        readonly IEventPublisher eventBus;

        [Inject]
        public DespawnAdventurerUseCase(IEventPublisher eventBus)
        {
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
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

            eventBus.Publish(new ActorDeparted(actor.Id, waitedDays));
        }
    }
}
