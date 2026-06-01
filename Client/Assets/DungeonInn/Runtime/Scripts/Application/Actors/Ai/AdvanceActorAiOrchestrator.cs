using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using VContainer;
using DungeonInn.Application.Actors.Ai;
using DungeonInn.Application.Actors.Phase;
using DungeonInn.Application.Actors.Lifecycle;
using DungeonInn.Application.Actors.Movement;
using DungeonInn.Application.Actors.Profiles;
using DungeonInn.Application.Actors.Spawn;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Dungeons;
using DungeonInn.Application.Economy;
using DungeonInn.Application.Facilities;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.Items;
using DungeonInn.Application.World;
using DungeonInn.Domain.Actor;

namespace DungeonInn.Application.Actors.Ai
{
    public sealed class AdvanceActorAiOrchestrator
    {
        readonly ActorDecisionScheduler scheduler;
        readonly IReadOnlyList<IActorAiPolicy> policies;
        readonly ApplyActorAiDecisionUseCase applyActorAiDecisionUseCase;
        readonly IActorActionPhaseStateStore phaseStateStore;

        [Inject]
        public AdvanceActorAiOrchestrator(
            ActorDecisionScheduler scheduler,
            AdventurerAiPolicy adventurerAiPolicy,
            MonsterAiPolicy monsterAiPolicy,
            PetAiPolicy petAiPolicy,
            GuildStaffAiPolicy guildStaffAiPolicy,
            ApplyActorAiDecisionUseCase applyActorAiDecisionUseCase,
            IActorActionPhaseStateStore phaseStateStore)
            : this(
                scheduler,
                new IActorAiPolicy[]
                {
                    adventurerAiPolicy ?? throw new ArgumentNullException(nameof(adventurerAiPolicy)),
                    monsterAiPolicy ?? throw new ArgumentNullException(nameof(monsterAiPolicy)),
                    petAiPolicy ?? throw new ArgumentNullException(nameof(petAiPolicy)),
                    guildStaffAiPolicy ?? throw new ArgumentNullException(nameof(guildStaffAiPolicy))
                },
                applyActorAiDecisionUseCase,
                phaseStateStore)
        {
        }

        internal AdvanceActorAiOrchestrator(
            ActorDecisionScheduler scheduler,
            IReadOnlyList<IActorAiPolicy> policies,
            ApplyActorAiDecisionUseCase applyActorAiDecisionUseCase,
            IActorActionPhaseStateStore phaseStateStore)
        {
            this.scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            this.policies = policies ?? throw new ArgumentNullException(nameof(policies));
            this.applyActorAiDecisionUseCase = applyActorAiDecisionUseCase ?? throw new ArgumentNullException(nameof(applyActorAiDecisionUseCase));
            this.phaseStateStore = phaseStateStore ?? throw new ArgumentNullException(nameof(phaseStateStore));
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
            return await ExecuteAsync(actors, null, currentTimeSeconds, evaluationFrameId, cooldownSeconds);
        }

        public async UniTask<bool> ExecuteAsync(
            IGameWorldStateReader worldState,
            float currentTimeSeconds,
            int evaluationFrameId,
            float cooldownSeconds)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            return await ExecuteAsync(
                worldState.Actors,
                worldState,
                currentTimeSeconds,
                evaluationFrameId,
                cooldownSeconds);
        }

        async UniTask<bool> ExecuteAsync(
            IEnumerable<Actor> actors,
            IGameWorldStateReader worldState,
            float currentTimeSeconds,
            int evaluationFrameId,
            float cooldownSeconds)
        {
            if (actors == null)
            {
                throw new ArgumentNullException(nameof(actors));
            }

            if (!TryGetEvaluationTarget(
                actors,
                currentTimeSeconds,
                evaluationFrameId,
                out var actor,
                out var runtimeState))
            {
                return false;
            }

            var policy = ResolvePolicy(actor);
            var context = new ActorAiContext(actor, currentTimeSeconds, runtimeState, worldState);
            var dirty = runtimeState.GetHighestDirty();
            try
            {
                var decision = Evaluate(policy, context, dirty);
                await applyActorAiDecisionUseCase.ExecuteAsync(actor, decision, currentTimeSeconds);
                runtimeState.ClearDirty(dirty);
                runtimeState.MarkDirty(decision.AdditionalDirtyFlags);
                runtimeState.MarkEvaluated(
                    currentTimeSeconds,
                    evaluationFrameId,
                    Math.Max(cooldownSeconds, decision.CooldownSeconds));
                return true;
            }
            catch
            {
                runtimeState.MarkEvaluated(currentTimeSeconds, evaluationFrameId, cooldownSeconds);
                throw;
            }
        }

        bool TryGetEvaluationTarget(
            IEnumerable<Actor> actors,
            float currentTimeSeconds,
            int evaluationFrameId,
            out Actor actor,
            out ActorAiRuntimeState runtimeState)
        {
            foreach (var candidate in actors)
            {
                if (phaseStateStore.IsActive(candidate.Id))
                {
                    continue;
                }

                var candidateState = scheduler.GetOrCreateState(candidate.Id);
                if (!candidateState.CanEvaluate(currentTimeSeconds, evaluationFrameId))
                {
                    continue;
                }

                actor = candidate;
                runtimeState = candidateState;
                return true;
            }

            actor = null;
            runtimeState = null;
            return false;
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
