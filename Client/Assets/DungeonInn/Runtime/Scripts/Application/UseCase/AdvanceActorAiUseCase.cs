using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.AI;
using DungeonInn.Domain.Actor;

namespace DungeonInn.Application.UseCase
{
    public sealed class AdvanceActorAiUseCase
    {
        readonly ActorDecisionScheduler scheduler;
        readonly IReadOnlyList<IActorAiPolicy> policies;
        readonly ApplyActorAiDecisionUseCase applyActorAiDecisionUseCase;

        public AdvanceActorAiUseCase()
            : this(
                new ActorDecisionScheduler(),
                new IActorAiPolicy[]
                {
                    new AdventurerAiPolicy(),
                    new MonsterAiPolicy(),
                    new PetAiPolicy(),
                    new GuildStaffAiPolicy()
                },
                new ApplyActorAiDecisionUseCase())
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

        public async UniTask<bool> ExecuteAsync(IEnumerable<Actor> actors, int currentTick, int cooldownTicks)
        {
            if (actors == null)
            {
                throw new ArgumentNullException(nameof(actors));
            }

            if (!scheduler.TryGetEvaluationTarget(actors, currentTick, out var actor, out var runtimeState))
            {
                return false;
            }

            var policy = ResolvePolicy(actor);
            var context = new ActorAiContext(actor, currentTick, runtimeState);
            var dirty = runtimeState.GetHighestDirty();
            try
            {
                var decision = Evaluate(policy, context, dirty);
                await applyActorAiDecisionUseCase.ExecuteAsync(actor, decision);
                runtimeState.ClearDirty(dirty);
                runtimeState.MarkDirty(decision.AdditionalDirtyFlags);
                runtimeState.MarkEvaluated(currentTick, cooldownTicks);
                return true;
            }
            catch
            {
                runtimeState.MarkEvaluated(currentTick, cooldownTicks);
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
