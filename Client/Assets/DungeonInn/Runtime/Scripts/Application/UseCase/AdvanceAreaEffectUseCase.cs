using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Event;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.Orchestration;
using DungeonInn.Domain.Combat;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class AdvanceAreaEffectUseCase
    {
        readonly AttackAreaTargetResolver targetResolver;
        readonly CombatEffectExecutor combatEffectExecutor;
        readonly ActorDefeatOrchestrator actorDefeatOrchestrator;
        readonly IEventPublisher eventPublisher;

        [Inject]
        public AdvanceAreaEffectUseCase(
            AttackAreaTargetResolver targetResolver,
            CombatEffectExecutor combatEffectExecutor,
            ActorDefeatOrchestrator actorDefeatOrchestrator,
            IEventPublisher eventPublisher)
        {
            this.targetResolver = targetResolver ?? throw new ArgumentNullException(nameof(targetResolver));
            this.combatEffectExecutor = combatEffectExecutor ?? throw new ArgumentNullException(nameof(combatEffectExecutor));
            this.actorDefeatOrchestrator = actorDefeatOrchestrator
                ?? throw new ArgumentNullException(nameof(actorDefeatOrchestrator));
            this.eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        }

        public UniTask ExecuteAsync(IGameWorldState worldState, float deltaGameSeconds)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            var bufferedEventPublisher = new BufferedEventPublisher(eventPublisher);
            for (var i = worldState.AreaEffects.Count - 1; 0 <= i; i--)
            {
                var areaEffect = worldState.AreaEffects[i];
                areaEffect.Advance(deltaGameSeconds);
                if (areaEffect.CanApply())
                {
                    ApplyAreaEffect(worldState, areaEffect, bufferedEventPublisher);
                    areaEffect.MarkApplied();
                }

                if (areaEffect.IsExpired)
                {
                    worldState.RemoveAreaEffect(areaEffect.Id);
                }
            }

            bufferedEventPublisher.Flush();
            return UniTask.CompletedTask;
        }

        void ApplyAreaEffect(
            IGameWorldState worldState,
            AreaEffectInstance areaEffect,
            IEventPublisher eventPublisher)
        {
            foreach (var target in targetResolver.ResolveTargets(areaEffect))
            {
                var targetDefeated = combatEffectExecutor.ExecuteAreaHit(worldState, areaEffect, target, eventPublisher);
                if (targetDefeated && worldState.FindActor(target.Id) != null)
                {
                    actorDefeatOrchestrator.Execute(
                        worldState,
                        worldState.FindActor(areaEffect.AttackerActorId),
                        target,
                        eventPublisher);
                }

                areaEffect.MarkHitActor(target.Id);
            }
        }
    }
}
