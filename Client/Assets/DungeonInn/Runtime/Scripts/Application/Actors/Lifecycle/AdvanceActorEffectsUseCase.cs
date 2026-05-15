using DungeonInn.Application.World;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using VContainer;

namespace DungeonInn.Application.Actors.Lifecycle
{
    public sealed class AdvanceActorEffectsUseCase
    {
        readonly ActorProcessingCandidateService candidateService;
        readonly List<Guid> actorIdBuffer = new();

        [Inject]
        public AdvanceActorEffectsUseCase(ActorProcessingCandidateService candidateService)
        {
            this.candidateService = candidateService ?? throw new ArgumentNullException(nameof(candidateService));
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

            candidateService.CollectActorEffectCandidates(actorIdBuffer);
            foreach (var actorId in actorIdBuffer)
            {
                var actor = worldState.FindActor(actorId);
                if (actor == null)
                {
                    candidateService.RemoveActor(actorId);
                    continue;
                }

                AdvanceActorEffects(actor, deltaGameSeconds);
                if (actor.ActorEffects.Count == 0)
                {
                    candidateService.ClearActorEffectCandidate(actor.Id);
                }
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
