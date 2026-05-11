using System;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.UseCase;
using DungeonInn.Domain.Actor;
using VContainer;

namespace DungeonInn.Application.Combat
{
    public sealed class CombatDefeatResolver
    {
        readonly IActorCombatService actorCombatService;
        readonly IEventPublisher eventBus;
        readonly GrantExperienceUseCase grantExperienceUseCase;
        readonly DropItemUseCase dropItemUseCase;

        [Inject]
        public CombatDefeatResolver(
            IActorCombatService actorCombatService,
            IEventPublisher eventBus,
            GrantExperienceUseCase grantExperienceUseCase,
            DropItemUseCase dropItemUseCase)
        {
            this.actorCombatService = actorCombatService ?? throw new ArgumentNullException(nameof(actorCombatService));
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            this.grantExperienceUseCase = grantExperienceUseCase ?? throw new ArgumentNullException(nameof(grantExperienceUseCase));
            this.dropItemUseCase = dropItemUseCase ?? throw new ArgumentNullException(nameof(dropItemUseCase));
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

            if (attacker != null)
            {
                grantExperienceUseCase.Execute(attacker, target);
            }

            dropItemUseCase.Execute(target, worldState);
            worldState.RemoveActor(target.Id);
            actorCombatService.ClearTargetsReferencing(target.Id);
            actorCombatService.RemoveState(target.Id);
            eventBus.Publish(new ActorDefeated(target.Id, attacker?.Id, DeathCause.Combat));
        }
    }
}
