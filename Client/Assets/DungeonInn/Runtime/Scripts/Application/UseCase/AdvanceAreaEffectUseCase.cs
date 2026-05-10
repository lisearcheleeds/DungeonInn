using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Combat;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Combat;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class AdvanceAreaEffectUseCase
    {
        readonly AttackAreaTargetResolver targetResolver;
        readonly CombatEffectExecutor combatEffectExecutor;

        [Inject]
        public AdvanceAreaEffectUseCase(
            AttackAreaTargetResolver targetResolver,
            CombatEffectExecutor combatEffectExecutor)
        {
            this.targetResolver = targetResolver ?? throw new ArgumentNullException(nameof(targetResolver));
            this.combatEffectExecutor = combatEffectExecutor ?? throw new ArgumentNullException(nameof(combatEffectExecutor));
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
            foreach (var target in targetResolver.ResolveTargets(worldState, areaEffect))
            {
                combatEffectExecutor.ExecuteAreaHit(worldState, areaEffect, target);
                areaEffect.MarkHitActor(target.Id);
            }
        }
    }
}
