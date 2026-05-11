using System;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using VContainer;

namespace DungeonInn.Application.Combat
{
    public sealed class CombatDefeatResolver
    {
        readonly IActorCombatService actorCombatService;
        readonly IEventPublisher eventBus;

        [Inject]
        public CombatDefeatResolver(
            IActorCombatService actorCombatService,
            IEventPublisher eventBus)
        {
            this.actorCombatService = actorCombatService ?? throw new ArgumentNullException(nameof(actorCombatService));
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public void Resolve(IGameWorldState worldState, Actor attacker, Actor target)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            foreach (var attackerId in actorCombatService.GetAttackers(target.Id))
            {
                eventBus.Publish(new CombatEncounterEnded(attackerId));
            }

            worldState.RemoveActor(target.Id);
            actorCombatService.ClearTargetsReferencing(target.Id);
            actorCombatService.RemoveState(target.Id);
            eventBus.Publish(new ActorDefeated(target.Id, attacker?.Id, DeathCause.Combat));
        }
    }
}
