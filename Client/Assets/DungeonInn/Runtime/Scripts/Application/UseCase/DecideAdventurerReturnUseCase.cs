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
using DungeonInn.Domain.Map;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class DecideAdventurerReturnUseCase : IDisposable
    {
        readonly IActorCombatService actorCombatService;
        readonly IGameEventBus eventBus;
        readonly IActorProfileRegistry profileRegistry;
        readonly Dictionary<Guid, Dictionary<int, int>> defeatedMonsterCountsByActor = new();
        readonly IDisposable defeatedSubscription;

        [Inject]
        public DecideAdventurerReturnUseCase(
            IActorCombatService actorCombatService,
            IGameEventBus eventBus,
            IActorProfileRegistry profileRegistry)
        {
            this.actorCombatService = actorCombatService ?? throw new ArgumentNullException(nameof(actorCombatService));
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            this.profileRegistry = profileRegistry ?? throw new ArgumentNullException(nameof(profileRegistry));
            defeatedSubscription = eventBus.OnEvent<ActorDefeated>().Subscribe(OnActorDefeated);
        }

        public void Dispose()
        {
            defeatedSubscription.Dispose();
        }

        public UniTask ExecuteAsync(IGameWorldState worldState)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            var actors = worldState.Actors;
            foreach (var actor in actors)
            {
                if (actor.Behavior is not AdventurerBehavior behavior)
                {
                    continue;
                }

                if (behavior.LifecycleState != AdventurerLifecycleState.Exploring)
                {
                    continue;
                }

                if (actorCombatService.HasTarget(actor.Id) || actorCombatService.IsTargetedByAny(actor.Id))
                {
                    continue;
                }

                if (!TryCompleteGoal(actor))
                {
                    continue;
                }

                actorCombatService.ClearCombatHistory(actor.Id);
                behavior.ChangeLifecycleState(AdventurerLifecycleState.Returning);
                eventBus.Publish(new ActorGoalCompleted(
                    actor.Id,
                    actor.CurrentGoal.Type,
                    actor.CurrentGoal.TargetId,
                    actor.CurrentGoal.ProgressCount,
                    actor.CurrentGoal.TargetCount));
                eventBus.Publish(new ActorStartedReturning(actor.Id));
            }

            return UniTask.CompletedTask;
        }

        void OnActorDefeated(ActorDefeated gameEvent)
        {
            if (!gameEvent.KillerActorId.HasValue)
            {
                return;
            }

            if (!profileRegistry.TryGetProfile(gameEvent.ActorId, out var profile) || profile.MonsterSpeciesId < 1)
            {
                return;
            }

            var actorId = gameEvent.KillerActorId.Value;
            if (!defeatedMonsterCountsByActor.TryGetValue(actorId, out var defeatedMonsterCounts))
            {
                defeatedMonsterCounts = new Dictionary<int, int>();
                defeatedMonsterCountsByActor.Add(actorId, defeatedMonsterCounts);
            }

            if (!defeatedMonsterCounts.TryGetValue(profile.MonsterSpeciesId, out var count))
            {
                count = 0;
            }

            defeatedMonsterCounts[profile.MonsterSpeciesId] = count + 1;
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
    }
}
