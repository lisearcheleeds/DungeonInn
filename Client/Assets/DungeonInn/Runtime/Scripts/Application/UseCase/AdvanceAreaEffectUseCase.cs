using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Combat;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class AdvanceAreaEffectUseCase
    {
        readonly IActorCombatService actorCombatService;
        readonly AttackAreaTargetResolver targetResolver;
        readonly IGameEventBus eventBus;
        readonly GrantExperienceUseCase grantExperienceUseCase;
        readonly DropItemUseCase dropItemUseCase;

        [Inject]
        public AdvanceAreaEffectUseCase(
            IActorCombatService actorCombatService,
            AttackAreaTargetResolver targetResolver,
            IGameEventBus eventBus,
            GrantExperienceUseCase grantExperienceUseCase,
            DropItemUseCase dropItemUseCase)
        {
            this.actorCombatService = actorCombatService ?? throw new ArgumentNullException(nameof(actorCombatService));
            this.targetResolver = targetResolver ?? throw new ArgumentNullException(nameof(targetResolver));
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            this.grantExperienceUseCase = grantExperienceUseCase ?? throw new ArgumentNullException(nameof(grantExperienceUseCase));
            this.dropItemUseCase = dropItemUseCase ?? throw new ArgumentNullException(nameof(dropItemUseCase));
        }

        public UniTask ExecuteAsync(IGameWorldState worldState, float deltaGameSeconds)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            var areaEffects = new List<AreaEffectInstance>(worldState.AreaEffects);
            foreach (var areaEffect in areaEffects)
            {
                areaEffect.Advance(deltaGameSeconds);
                if (areaEffect.CanApply())
                {
                    ApplyAreaEffect(worldState, areaEffect);
                    areaEffect.MarkApplied();
                }

                if (areaEffect.IsExpired)
                {
                    worldState.RemoveAreaEffect(areaEffect.Id);
                }
            }

            return UniTask.CompletedTask;
        }

        void ApplyAreaEffect(IGameWorldState worldState, AreaEffectInstance areaEffect)
        {
            var attacker = worldState.FindActor(areaEffect.AttackerActorId);
            foreach (var target in targetResolver.ResolveTargets(worldState, areaEffect))
            {
                target.ReceiveDamage(areaEffect.Damage);
                areaEffect.MarkHitActor(target.Id);
                eventBus.Publish(new AreaEffectHit(areaEffect.Id, areaEffect.AttackerActorId, target.Id, areaEffect.Damage));
                eventBus.Publish(new CombatAttackOccurred(
                    areaEffect.AttackerActorId,
                    target.Id,
                    areaEffect.Damage,
                    target.Hp));

                if (attacker != null)
                {
                    actorCombatService.MarkCombatParticipation(attacker.Id);
                }

                actorCombatService.MarkCombatParticipation(target.Id);
                if (target.Hp <= 0)
                {
                    ResolveDefeat(worldState, attacker, target);
                }
            }
        }

        void ResolveDefeat(IGameWorldState worldState, Actor attacker, Actor target)
        {
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
