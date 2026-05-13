using DungeonInn.Application.World;
using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using VContainer;

namespace DungeonInn.Application.Actors.Lifecycle
{
    public sealed class AdvanceActorEffectsUseCase
    {
        [Inject]
        public AdvanceActorEffectsUseCase()
        {
        }

        public UniTask ExecuteAsync(IGameWorldState worldState, float deltaGameSeconds)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            if (deltaGameSeconds <= 0f)
            {
                return UniTask.CompletedTask;
            }

            foreach (var actor in worldState.Actors)
            {
                AdvanceActorEffects(actor, deltaGameSeconds);
            }

            return UniTask.CompletedTask;
        }

        static void AdvanceActorEffects(Actor actor, float deltaGameSeconds)
        {
            foreach (var actorEffect in actor.ActorEffects)
            {
                actorEffect.Advance(deltaGameSeconds);
                foreach (var statusEffect in actorEffect.StatusEffects)
                {
                    ApplyStatusEffect(actor, statusEffect, deltaGameSeconds);
                }
            }

            actor.RemoveExpiredActorEffects();
        }

        static void ApplyStatusEffect(Actor actor, ActiveStatusEffect statusEffect, float deltaGameSeconds)
        {
            switch (statusEffect.Type)
            {
                case StatusEffectType.HealHpOverTime:
                    actor.Recover(statusEffect.Advance(deltaGameSeconds), 0, 0, 0, 0);
                    return;
                case StatusEffectType.MoveSpeedDown:
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(statusEffect));
            }
        }
    }
}
