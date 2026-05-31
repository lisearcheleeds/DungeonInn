using DungeonInn.Application.World;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.Actors.Ai;
using DungeonInn.Application.Actors.Lifecycle;
using DungeonInn.Domain.Actor;
using VContainer;

namespace DungeonInn.Application.Actors.Lifecycle
{
    public sealed class DecideAdventurerReturnUseCase
    {
        readonly IActorCombatService actorCombatService;
        readonly IEventPublisher eventPublisher;
        readonly AdventurerReturnTrackingService returnTrackingService;
        readonly AdventureGoalProgressService goalProgressService;
        readonly ActorProcessingCandidateService candidateService;
        readonly IWorldGameSettingsRepository worldGameSettingsRepository;
        readonly RecoveryItemCandidateQuery recoveryItemCandidateQuery;
        readonly RecoveryEffectEstimator recoveryEffectEstimator;
        readonly List<Guid> actorIdBuffer = new();

        [Inject]
        public DecideAdventurerReturnUseCase(
            IActorCombatService actorCombatService,
            IEventPublisher eventPublisher,
            AdventurerReturnTrackingService returnTrackingService,
            AdventureGoalProgressService goalProgressService,
            ActorProcessingCandidateService candidateService,
            IWorldGameSettingsRepository worldGameSettingsRepository,
            RecoveryItemCandidateQuery recoveryItemCandidateQuery,
            RecoveryEffectEstimator recoveryEffectEstimator)
        {
            this.actorCombatService = actorCombatService ?? throw new ArgumentNullException(nameof(actorCombatService));
            this.eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            this.returnTrackingService = returnTrackingService ?? throw new ArgumentNullException(nameof(returnTrackingService));
            this.goalProgressService = goalProgressService ?? throw new ArgumentNullException(nameof(goalProgressService));
            this.candidateService = candidateService ?? throw new ArgumentNullException(nameof(candidateService));
            this.worldGameSettingsRepository =
                worldGameSettingsRepository ?? throw new ArgumentNullException(nameof(worldGameSettingsRepository));
            this.recoveryItemCandidateQuery =
                recoveryItemCandidateQuery ?? throw new ArgumentNullException(nameof(recoveryItemCandidateQuery));
            this.recoveryEffectEstimator =
                recoveryEffectEstimator ?? throw new ArgumentNullException(nameof(recoveryEffectEstimator));
        }

        public UniTask ExecuteAsync(IGameWorldState worldState)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            if (!returnTrackingService.HasDirtyActors)
            {
                return UniTask.CompletedTask;
            }

            var foundDirtyActorIds = new HashSet<Guid>();
            returnTrackingService.CollectDirtyActorIds(actorIdBuffer);
            foreach (var actorId in actorIdBuffer)
            {
                var actor = worldState.FindActor(actorId);
                if (actor == null)
                {
                    returnTrackingService.ClearDirty(actorId);
                    continue;
                }

                foundDirtyActorIds.Add(actor.Id);

                if (actor.Behavior is not AdventurerBehavior behavior)
                {
                    returnTrackingService.ClearDirty(actor.Id);
                    continue;
                }

                if (behavior.LifecycleState != AdventurerLifecycleState.Exploring)
                {
                    returnTrackingService.ClearDirty(actor.Id);
                    continue;
                }

                if (actorCombatService.HasTarget(actor.Id) || actorCombatService.IsTargetedByAny(actor.Id))
                {
                    continue;
                }

                var returnDecision = CalculateReturnDecision(actor);
                if (returnDecision.Score < worldGameSettingsRepository.GetAdventurerReturnPolicySettings().DecisionThresholdScore)
                {
                    returnTrackingService.ClearDirty(actor.Id);
                    continue;
                }

                actorCombatService.ClearCombatHistory(actor.Id);
                behavior.ChangeLifecycleState(AdventurerLifecycleState.Returning);
                candidateService.MarkPostDungeonScheduleCandidates(actor.Id);
                returnTrackingService.ClearDirty(actor.Id);

                if (returnDecision.GoalCompleted)
                {
                    eventPublisher.Publish(new ActorGoalCompleted(
                        actor.Id,
                        actor.CurrentGoal.Type,
                        actor.CurrentGoal.TargetId,
                        actor.CurrentGoal.ProgressCount,
                        actor.CurrentGoal.TargetCount));
                }

                eventPublisher.Publish(new ActorAiDecisionRecorded(
                    actor.Id,
                    AiDecisionType.ReturnToInn,
                    returnDecision.ReasonType,
                    default,
                    default,
                    currentHp: actor.Hp,
                    maxHp: actor.Params.MaxHp,
                    selectedFloor: 0,
                    score: returnDecision.Score));
                eventPublisher.Publish(new ActorStartedReturning(actor.Id));
            }

            returnTrackingService.RemoveMissingDirtyActors(foundDirtyActorIds);
            return UniTask.CompletedTask;
        }

        AdventurerReturnDecision CalculateReturnDecision(Actor actor)
        {
            var returnPolicySettings = worldGameSettingsRepository.GetAdventurerReturnPolicySettings();
            var score = 0;
            var goalCompleted = goalProgressService.UpdateProgress(actor);
            if (goalCompleted)
            {
                score += returnPolicySettings.GoalCompletedScore;
            }

            var candidates = recoveryItemCandidateQuery.Execute(actor);
            var estimatedHp = HasActiveRecoveryEffect(actor)
                ? actor.Params.MaxHp
                : recoveryEffectEstimator.EstimateHpAfterRecovery(actor, candidates);
            var hpRatio = actor.Hp / (float)actor.Params.MaxHp;
            var estimatedHpRatio = estimatedHp / (float)actor.Params.MaxHp;
            if (hpRatio <= returnPolicySettings.CriticalHpRatio)
            {
                score += returnPolicySettings.CriticalHpScore;
            }
            else if (hpRatio <= returnPolicySettings.LowHpRatio && estimatedHpRatio <= returnPolicySettings.LowHpRatio)
            {
                score += returnPolicySettings.LowHpWithoutRecoveryItemScore;
            }

            var reasonType = AiDecisionReasonType.None;
            if (goalCompleted)
            {
                reasonType = AiDecisionReasonType.GoalCompleted;
            }
            else if (hpRatio <= returnPolicySettings.CriticalHpRatio)
            {
                reasonType = AiDecisionReasonType.CriticalHp;
            }
            else if (hpRatio <= returnPolicySettings.LowHpRatio && estimatedHpRatio <= returnPolicySettings.LowHpRatio)
            {
                reasonType = AiDecisionReasonType.LowHpWithoutRecoveryItem;
            }

            return new AdventurerReturnDecision(score, goalCompleted, reasonType);
        }

        static bool HasActiveRecoveryEffect(Actor actor)
        {
            foreach (var actorEffect in actor.ActorEffects)
            {
                foreach (var statusEffect in actorEffect.StatusEffects)
                {
                    if (statusEffect.Type == StatusEffectType.HealHpOverTime && !statusEffect.IsExpired)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        readonly struct AdventurerReturnDecision
        {
            public int Score { get; }
            public bool GoalCompleted { get; }
            public AiDecisionReasonType ReasonType { get; }

            public AdventurerReturnDecision(int score, bool goalCompleted, AiDecisionReasonType reasonType)
            {
                Score = score;
                GoalCompleted = goalCompleted;
                ReasonType = reasonType;
            }
        }
    }
}
