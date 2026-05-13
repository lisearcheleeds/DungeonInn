using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Combat;
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

        [Inject]
        public AdvanceAreaEffectUseCase(
            AttackAreaTargetResolver targetResolver,
            CombatEffectExecutor combatEffectExecutor,
            ActorDefeatOrchestrator actorDefeatOrchestrator)
        {
            this.targetResolver = targetResolver ?? throw new ArgumentNullException(nameof(targetResolver));
            this.combatEffectExecutor = combatEffectExecutor ?? throw new ArgumentNullException(nameof(combatEffectExecutor));
            this.actorDefeatOrchestrator = actorDefeatOrchestrator
                ?? throw new ArgumentNullException(nameof(actorDefeatOrchestrator));
        }

        public UniTask ExecuteAsync(IGameWorldState worldState, float deltaGameSeconds)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            for (var i = worldState.AreaEffects.Count - 1; 0 <= i; i--)
            {
                var areaEffect = worldState.AreaEffects[i];
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
            foreach (var target in targetResolver.ResolveTargets(worldState, areaEffect))
            {
                var targetDefeated = combatEffectExecutor.ExecuteAreaHit(worldState, areaEffect, target);
                if (targetDefeated && worldState.FindActor(target.Id) != null)
                {
                    actorDefeatOrchestrator.Execute(
                        worldState,
                        worldState.FindActor(areaEffect.AttackerActorId),
                        target);
                }

                areaEffect.MarkHitActor(target.Id);
            }
        }
    }
}
