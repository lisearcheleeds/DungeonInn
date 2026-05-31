using System;
using DungeonInn.Application.Actors.Lifecycle;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.World;
using DungeonInn.Domain.Actor;
using VContainer;

namespace DungeonInn.Application.Combat
{
    public sealed class CombatDefeatResolver
    {
        readonly IActorCombatService actorCombatService;
        readonly AdventurerDeathRevivalService adventurerDeathRevivalService;

        [Inject]
        public CombatDefeatResolver(
            IActorCombatService actorCombatService,
            AdventurerDeathRevivalService adventurerDeathRevivalService)
        {
            this.actorCombatService = actorCombatService ?? throw new ArgumentNullException(nameof(actorCombatService));
            this.adventurerDeathRevivalService =
                adventurerDeathRevivalService ?? throw new ArgumentNullException(nameof(adventurerDeathRevivalService));
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

            actorCombatService.ClearTargetsReferencing(target.Id);
            actorCombatService.RemoveState(target.Id);
            if (adventurerDeathRevivalService.CanReviveAtInn(worldState, target))
            {
                worldState.RemoveActor(target.Id);
                eventPublisher.Publish(new ActorDefeated(target.Id, attacker?.Id, DeathCause.Combat));
                adventurerDeathRevivalService.ReviveAtInn(worldState, target, eventPublisher);
                return;
            }

            worldState.RemoveActor(target.Id);
            eventPublisher.Publish(new ActorDefeated(target.Id, attacker?.Id, DeathCause.Combat));
        }
    }
}
