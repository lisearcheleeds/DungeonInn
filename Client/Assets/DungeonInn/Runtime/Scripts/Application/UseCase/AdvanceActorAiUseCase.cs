using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.AI;
using DungeonInn.Domain.Actor;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class AdvanceActorAiUseCase
    {
        readonly ActorDecisionScheduler scheduler;
        readonly IReadOnlyList<IActorAiPolicy> policies;
        readonly ApplyActorAiDecisionUseCase applyActorAiDecisionUseCase;

        [Inject]
        public AdvanceActorAiUseCase(
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

        public AdvanceActorAiUseCase(
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
            var policy = policies.FirstOrDefault(x => x.CanHandle(actor));
            if (policy == null)
            {
                throw new InvalidOperationException("Actor AI policy does not exist.");
            }

            return policy;
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
