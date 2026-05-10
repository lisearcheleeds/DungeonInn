using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.Profiles;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class DecideAdventurerReturnUseCase : IDisposable
    {
        readonly IActorCombatService actorCombatService;
        readonly IGameEventBus eventBus;
        readonly IActorProfileRegistry profileRegistry;
        readonly IItemMasterRepository itemMasterRepository;
        readonly Dictionary<Guid, Dictionary<int, int>> defeatedMonsterCountsByActor = new();
        readonly HashSet<Guid> dirtyActorIds = new();
        DisposableBag bag;

        [Inject]
        public DecideAdventurerReturnUseCase(
            IActorCombatService actorCombatService,
            IGameEventBus eventBus,
            IActorProfileRegistry profileRegistry,
            IItemMasterRepository itemMasterRepository)
        {
            this.actorCombatService = actorCombatService ?? throw new ArgumentNullException(nameof(actorCombatService));
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            this.profileRegistry = profileRegistry ?? throw new ArgumentNullException(nameof(profileRegistry));
            this.itemMasterRepository = itemMasterRepository ?? throw new ArgumentNullException(nameof(itemMasterRepository));
            eventBus.OnEvent<ActorDefeated>()
                .Subscribe(OnActorDefeated)
                .AddTo(ref bag);
            eventBus.OnEvent<CombatEncounterEnded>()
                .Subscribe(gameEvent => MarkDirty(gameEvent.ActorId))
                .AddTo(ref bag);
            eventBus.OnEvent<ItemPickedUp>()
                .Subscribe(gameEvent => MarkDirty(gameEvent.ActorId))
                .AddTo(ref bag);
            eventBus.OnEvent<ActorLeveledUp>()
                .Subscribe(gameEvent => MarkDirty(gameEvent.ActorId))
                .AddTo(ref bag);
            eventBus.OnEvent<ActorEnteredDungeon>()
                .Subscribe(gameEvent => MarkDirty(gameEvent.ActorId))
                .AddTo(ref bag);
        }

        public void Dispose()
        {
            bag.Dispose();
        }

        public UniTask ExecuteAsync(IGameWorldState worldState)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            if (dirtyActorIds.Count == 0)
            {
                return UniTask.CompletedTask;
            }

            var actors = worldState.Actors;
            var foundDirtyActorIds = new HashSet<Guid>();
            foreach (var actor in actors)
            {
                if (!dirtyActorIds.Contains(actor.Id))
                {
                    continue;
                }

                foundDirtyActorIds.Add(actor.Id);

                if (actor.Behavior is not AdventurerBehavior behavior)
                {
                    dirtyActorIds.Remove(actor.Id);
                    continue;
                }

                if (behavior.LifecycleState != AdventurerLifecycleState.Exploring)
                {
                    dirtyActorIds.Remove(actor.Id);
                    continue;
                }

                if (actorCombatService.HasTarget(actor.Id) || actorCombatService.IsTargetedByAny(actor.Id))
                {
                    continue;
                }

                var returnDecision = CalculateReturnDecision(actor);
                if (returnDecision.Score < GameConstants.AdventurerReturnDecisionThresholdScore)
                {
                    dirtyActorIds.Remove(actor.Id);
                    continue;
                }

                actorCombatService.ClearCombatHistory(actor.Id);
                behavior.ChangeLifecycleState(AdventurerLifecycleState.Returning);
                dirtyActorIds.Remove(actor.Id);

                if (returnDecision.GoalCompleted)
                {
                    eventBus.Publish(new ActorGoalCompleted(
                        actor.Id,
                        actor.CurrentGoal.Type,
                        actor.CurrentGoal.TargetId,
                        actor.CurrentGoal.ProgressCount,
                        actor.CurrentGoal.TargetCount));
                }

                eventBus.Publish(new ActorStartedReturning(actor.Id));
            }

            RemoveMissingDirtyActors(foundDirtyActorIds);
            return UniTask.CompletedTask;
        }

        void MarkDirty(Guid actorId)
        {
            dirtyActorIds.Add(actorId);
        }

        void RemoveMissingDirtyActors(HashSet<Guid> foundDirtyActorIds)
        {
            var dirtyIds = new List<Guid>(dirtyActorIds);
            foreach (var actorId in dirtyIds)
            {
                if (!foundDirtyActorIds.Contains(actorId))
                {
                    dirtyActorIds.Remove(actorId);
                }
            }
        }

        AdventurerReturnDecision CalculateReturnDecision(Actor actor)
        {
            var score = 0;
            var goalCompleted = TryCompleteGoal(actor);
            if (goalCompleted)
            {
                score += GameConstants.AdventurerReturnGoalCompletedScore;
            }

            var hpRatio = actor.Hp / (float)actor.Params.MaxHp;
            if (hpRatio <= GameConstants.AdventurerReturnCriticalHpRatio)
            {
                score += GameConstants.AdventurerReturnCriticalHpScore;
            }
            else if (hpRatio <= GameConstants.AdventurerReturnLowHpRatio && !HasRecoveryItem(actor))
            {
                score += GameConstants.AdventurerReturnLowHpWithoutRecoveryItemScore;
            }

            return new AdventurerReturnDecision(score, goalCompleted);
        }

        void OnActorDefeated(ActorDefeated gameEvent)
        {
            if (gameEvent.KillerActorId.HasValue)
            {
                MarkDirty(gameEvent.KillerActorId.Value);
            }

            if (!gameEvent.KillerActorId.HasValue)
            {
                return;
            }

            if (!profileRegistry.TryGetProfile(gameEvent.ActorId, out var profile) || profile.SpeciesId < 1)
            {
                return;
            }

            var actorId = gameEvent.KillerActorId.Value;
            if (!defeatedMonsterCountsByActor.TryGetValue(actorId, out var defeatedMonsterCounts))
            {
                defeatedMonsterCounts = new Dictionary<int, int>();
                defeatedMonsterCountsByActor.Add(actorId, defeatedMonsterCounts);
            }

            if (!defeatedMonsterCounts.TryGetValue(profile.SpeciesId, out var count))
            {
                count = 0;
            }

            defeatedMonsterCounts[profile.SpeciesId] = count + 1;
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
            if (!defeatedMonsterCountsByActor.TryGetValue(actor.Id, out var defeatedMonsterCounts))
            {
                actor.CurrentGoal.SetProgress(0);
                return false;
            }

            defeatedMonsterCounts.TryGetValue(actor.CurrentGoal.TargetId, out var count);
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

        readonly struct AdventurerReturnDecision
        {
            public int Score { get; }
            public bool GoalCompleted { get; }

            public AdventurerReturnDecision(int score, bool goalCompleted)
            {
                Score = score;
                GoalCompleted = goalCompleted;
            }
        }
    }
}
