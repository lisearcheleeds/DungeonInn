using System;
using DungeonInn.Application.UseCase;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.Combat;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.AI;
using DungeonInn.Domain.Actor;
using VContainer;

namespace DungeonInn.Application.Orchestration
{
    public sealed class AdvanceActorAiOrchestrator
    {
        readonly ActorDecisionScheduler scheduler;
        readonly IReadOnlyList<IActorAiPolicy> policies;
        readonly ApplyActorAiDecisionUseCase applyActorAiDecisionUseCase;

        [Inject]
        public AdvanceActorAiOrchestrator(
            ActorDecisionScheduler scheduler,
            AdventurerAiPolicy adventurerAiPolicy,
            MonsterAiPolicy monsterAiPolicy,
            PetAiPolicy petAiPolicy,
            GuildStaffAiPolicy guildStaffAiPolicy,
            ApplyActorAiDecisionUseCase applyActorAiDecisionUseCase)
            : this(
                scheduler,
                new IActorAiPolicy[]
                {
                    adventurerAiPolicy ?? throw new ArgumentNullException(nameof(adventurerAiPolicy)),
                    monsterAiPolicy ?? throw new ArgumentNullException(nameof(monsterAiPolicy)),
                    petAiPolicy ?? throw new ArgumentNullException(nameof(petAiPolicy)),
                    guildStaffAiPolicy ?? throw new ArgumentNullException(nameof(guildStaffAiPolicy))
                },
                applyActorAiDecisionUseCase)
        {
        }

        public AdvanceActorAiOrchestrator(
            ActorDecisionScheduler scheduler,
            IReadOnlyList<IActorAiPolicy> policies,
            ApplyActorAiDecisionUseCase applyActorAiDecisionUseCase)
        {
            this.scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            this.policies = policies ?? throw new ArgumentNullException(nameof(policies));
            this.applyActorAiDecisionUseCase = applyActorAiDecisionUseCase ?? throw new ArgumentNullException(nameof(applyActorAiDecisionUseCase));
        }

        public UniTask MarkEventAsync(Guid actorId, ActorAiEventType eventType)
        {
            scheduler.MarkEvent(actorId, eventType);
            return UniTask.CompletedTask;
        }

        public async UniTask<bool> ExecuteAsync(
            IEnumerable<Actor> actors,
            float currentTimeSeconds,
            int evaluationFrameId,
            float cooldownSeconds)
        {
            if (actors == null)
            {
                throw new ArgumentNullException(nameof(actors));
            }

            if (!scheduler.TryGetEvaluationTarget(actors, currentTimeSeconds, evaluationFrameId, out var actor, out var runtimeState))
            {
                return false;
            }

            var policy = ResolvePolicy(actor);
            var context = new ActorAiContext(actor, currentTimeSeconds, runtimeState);
            var dirty = runtimeState.GetHighestDirty();
            try
            {
                var decision = Evaluate(policy, context, dirty);
                await applyActorAiDecisionUseCase.ExecuteAsync(actor, decision);
                runtimeState.ClearDirty(dirty);
                runtimeState.MarkDirty(decision.AdditionalDirtyFlags);
                runtimeState.MarkEvaluated(currentTimeSeconds, evaluationFrameId, cooldownSeconds);
                return true;
            }
            catch
            {
                runtimeState.MarkEvaluated(currentTimeSeconds, evaluationFrameId, cooldownSeconds);
                throw;
            }
        }

        IActorAiPolicy ResolvePolicy(Actor actor)
        {
            foreach (var policy in policies)
            {
                if (policy.CanHandle(actor))
                {
                    return policy;
                }
            }

            throw new InvalidOperationException("Actor AI policy does not exist.");
        }

        static ActorAiDecision Evaluate(IActorAiPolicy policy, ActorAiContext context, ActorAiDirtyFlags dirty)
        {
            switch (dirty)
            {
                case ActorAiDirtyFlags.LongTerm:
                    return policy.EvaluateLongTerm(context);
                case ActorAiDirtyFlags.MidTerm:
                    return policy.EvaluateMidTerm(context);
                case ActorAiDirtyFlags.ShortTerm:
                    return policy.EvaluateShortTerm(context);
                case ActorAiDirtyFlags.None:
                    return ActorAiDecision.None();
                default:
                    throw new ArgumentOutOfRangeException(nameof(dirty));
            }
        }
    }
}
