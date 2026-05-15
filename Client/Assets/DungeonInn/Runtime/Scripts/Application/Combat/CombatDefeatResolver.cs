using DungeonInn.Application.World;
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

        [Inject]
        public CombatDefeatResolver(IActorCombatService actorCombatService)
        {
            this.actorCombatService = actorCombatService ?? throw new ArgumentNullException(nameof(actorCombatService));
        }

        public void Resolve(IGameWorldState worldState, Actor attacker, Actor target, IEventPublisher eventPublisher)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (eventPublisher == null)
            {
                throw new ArgumentNullException(nameof(eventPublisher));
            }

            foreach (var attackerId in actorCombatService.GetAttackers(target.Id))
            {
                eventPublisher.Publish(new CombatEncounterEnded(attackerId));
            }

            worldState.RemoveActor(target.Id);
            actorCombatService.ClearTargetsReferencing(target.Id);
            actorCombatService.RemoveState(target.Id);
            eventPublisher.Publish(new ActorDefeated(target.Id, attacker?.Id, DeathCause.Combat));
        }
    }
}
