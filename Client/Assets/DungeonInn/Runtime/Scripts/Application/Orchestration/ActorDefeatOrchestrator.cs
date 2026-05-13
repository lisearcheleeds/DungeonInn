using System;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Event;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.UseCase;
using DungeonInn.Domain.Actor;
using VContainer;

namespace DungeonInn.Application.Orchestration
{
    public sealed class ActorDefeatOrchestrator
    {
        readonly CombatDefeatResolver combatDefeatResolver;
        readonly GrantExperienceUseCase grantExperienceUseCase;
        readonly DropItemUseCase dropItemUseCase;

        [Inject]
        public ActorDefeatOrchestrator(
            CombatDefeatResolver combatDefeatResolver,
            GrantExperienceUseCase grantExperienceUseCase,
            DropItemUseCase dropItemUseCase)
        {
            this.combatDefeatResolver = combatDefeatResolver ?? throw new ArgumentNullException(nameof(combatDefeatResolver));
            this.grantExperienceUseCase = grantExperienceUseCase ?? throw new ArgumentNullException(nameof(grantExperienceUseCase));
            this.dropItemUseCase = dropItemUseCase ?? throw new ArgumentNullException(nameof(dropItemUseCase));
        }

        public void Execute(IGameWorldState worldState, Actor attacker, Actor target)
        {
            if (attacker != null)
            {
                grantExperienceUseCase.Execute(attacker, target);
            }

            dropItemUseCase.Execute(target, worldState);
            combatDefeatResolver.Resolve(worldState, attacker, target);
        }

        public void Execute(IGameWorldState worldState, Actor attacker, Actor target, IEventPublisher eventPublisher)
        {
            if (eventPublisher == null)
            {
                throw new ArgumentNullException(nameof(eventPublisher));
            }

            if (attacker != null)
            {
                grantExperienceUseCase.Execute(attacker, target, eventPublisher);
            }

            dropItemUseCase.Execute(target, worldState, eventPublisher);
            combatDefeatResolver.Resolve(worldState, attacker, target, eventPublisher);
        }
    }
}
