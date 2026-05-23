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
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.Actors.Lifecycle
{
    public sealed class DecideAdventurerReturnUseCase
    {
        readonly IActorCombatService actorCombatService;
        readonly IEventPublisher eventPublisher;
        readonly AdventurerReturnTrackingService returnTrackingService;
        readonly IItemMasterRepository itemMasterRepository;
        readonly ActorProcessingCandidateService candidateService;
        readonly AdventurerReturnPolicySettings returnPolicySettings;
        readonly List<Guid> actorIdBuffer = new();

        [Inject]
        public DecideAdventurerReturnUseCase(
            IActorCombatService actorCombatService,
            IEventPublisher eventPublisher,
            AdventurerReturnTrackingService returnTrackingService,
            IItemMasterRepository itemMasterRepository,
            ActorProcessingCandidateService candidateService,
            AdventurerReturnPolicySettings returnPolicySettings)
        {
            this.actorCombatService = actorCombatService ?? throw new ArgumentNullException(nameof(actorCombatService));
            this.eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            this.returnTrackingService = returnTrackingService ?? throw new ArgumentNullException(nameof(returnTrackingService));
            this.itemMasterRepository = itemMasterRepository ?? throw new ArgumentNullException(nameof(itemMasterRepository));
            this.candidateService = candidateService ?? throw new ArgumentNullException(nameof(candidateService));
            this.returnPolicySettings = returnPolicySettings
                ?? throw new ArgumentNullException(nameof(returnPolicySettings));
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
                if (returnDecision.Score < returnPolicySettings.DecisionThresholdScore)
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
            var score = 0;
            var goalCompleted = TryCompleteGoal(actor);
            if (goalCompleted)
            {
                score += returnPolicySettings.GoalCompletedScore;
            }

            var hpRatio = actor.Hp / (float)actor.Params.MaxHp;
            if (hpRatio <= returnPolicySettings.CriticalHpRatio)
            {
                score += returnPolicySettings.CriticalHpScore;
            }
            else if (hpRatio <= returnPolicySettings.LowHpRatio && !HasRecoveryItem(actor))
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
            else if (hpRatio <= returnPolicySettings.LowHpRatio && !HasRecoveryItem(actor))
            {
                reasonType = AiDecisionReasonType.LowHpWithoutRecoveryItem;
            }

            return new AdventurerReturnDecision(score, goalCompleted, reasonType);
        }

        bool TryCompleteGoal(Actor actor)
        {
            switch (actor.CurrentGoal.Type)
            {
                case ActorGoalType.LevelUp:
                    return TryCompleteLevelUpGoal(actor);
                case ActorGoalType.CollectItem:
                    return TryCompleteCollectItemGoal(actor);
                case ActorGoalType.DefeatMonster:
                    return TryCompleteDefeatMonsterGoal(actor);
                case ActorGoalType.ReachFloor:
                    return TryCompleteReachFloorGoal(actor);
                case ActorGoalType.None:
                    return false;
                default:
                    return false;
            }
        }

        bool TryCompleteLevelUpGoal(Actor actor)
        {
            if (!actorCombatService.HasParticipatedInCombat(actor.Id))
            {
                return false;
            }

            actor.CurrentGoal.SetProgress(Math.Max(1, actor.CurrentGoal.TargetCount));
            return true;
        }

        static bool TryCompleteCollectItemGoal(Actor actor)
        {
            actor.Inventory.ItemCounts.TryGetValue(actor.CurrentGoal.TargetId, out var count);
            actor.CurrentGoal.SetProgress(count);
            return actor.CurrentGoal.IsCompleted();
        }

        bool TryCompleteDefeatMonsterGoal(Actor actor)
        {
            var count = returnTrackingService.GetDefeatedMonsterCount(actor.Id, actor.CurrentGoal.TargetId);
            actor.CurrentGoal.SetProgress(count);
            return actor.CurrentGoal.IsCompleted();
        }

        static bool TryCompleteReachFloorGoal(Actor actor)
        {
            if (actor.Position.LayerId.Equals(MapLayerId.Ground))
            {
                actor.CurrentGoal.SetProgress(0);
                return false;
            }

            var progress = actor.CurrentGoal.TargetId <= actor.Position.LayerId.Value ? 1 : 0;
            actor.CurrentGoal.SetProgress(progress);
            return actor.CurrentGoal.IsCompleted();
        }

        bool HasRecoveryItem(Actor actor)
        {
            if (HasActiveRecoveryEffect(actor))
            {
                return true;
            }

            foreach (var kvp in actor.Inventory.ItemCounts)
            {
                if (kvp.Value < 1)
                {
                    continue;
                }

                if (itemMasterRepository.GetItemMaster(kvp.Key).Category == ItemCategory.Consumable)
                {
                    return true;
                }
            }

            return false;
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
