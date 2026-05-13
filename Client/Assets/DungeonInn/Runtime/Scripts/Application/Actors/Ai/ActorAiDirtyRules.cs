using System;
using System.Collections.Generic;
using DungeonInn.Domain.Actor;

namespace DungeonInn.Application.Actors.Ai
{
    public sealed class ActorAiDirtyRules
    {
        readonly IReadOnlyDictionary<ActorGoalType, ActorAiDirtyFlags> goalDirtyFlags = new Dictionary<ActorGoalType, ActorAiDirtyFlags>
        {
            { ActorGoalType.None, ActorAiDirtyFlags.None },
            { ActorGoalType.LevelUp, ActorAiDirtyFlags.MidTerm | ActorAiDirtyFlags.ShortTerm },
            { ActorGoalType.CollectItem, ActorAiDirtyFlags.MidTerm | ActorAiDirtyFlags.ShortTerm },
            { ActorGoalType.DefeatMonster, ActorAiDirtyFlags.MidTerm | ActorAiDirtyFlags.ShortTerm },
            { ActorGoalType.ReachFloor, ActorAiDirtyFlags.MidTerm | ActorAiDirtyFlags.ShortTerm },
            { ActorGoalType.WorkAtFacility, ActorAiDirtyFlags.MidTerm | ActorAiDirtyFlags.ShortTerm },
            { ActorGoalType.Patrol, ActorAiDirtyFlags.MidTerm | ActorAiDirtyFlags.ShortTerm },
            { ActorGoalType.Recover, ActorAiDirtyFlags.MidTerm | ActorAiDirtyFlags.ShortTerm },
            { ActorGoalType.Leave, ActorAiDirtyFlags.MidTerm | ActorAiDirtyFlags.ShortTerm }
        };

        readonly IReadOnlyDictionary<ActorPlanType, ActorAiDirtyFlags> planDirtyFlags = new Dictionary<ActorPlanType, ActorAiDirtyFlags>
        {
            { ActorPlanType.None, ActorAiDirtyFlags.None },
            { ActorPlanType.Prepare, ActorAiDirtyFlags.ShortTerm },
            { ActorPlanType.ExploreCurrentArea, ActorAiDirtyFlags.ShortTerm },
            { ActorPlanType.TravelToTarget, ActorAiDirtyFlags.ShortTerm },
            { ActorPlanType.Recover, ActorAiDirtyFlags.ShortTerm },
            { ActorPlanType.UseFacility, ActorAiDirtyFlags.ShortTerm },
            { ActorPlanType.WorkAtFacility, ActorAiDirtyFlags.ShortTerm },
            { ActorPlanType.Patrol, ActorAiDirtyFlags.ShortTerm },
            { ActorPlanType.Leave, ActorAiDirtyFlags.ShortTerm }
        };

        readonly IReadOnlyDictionary<ActorActionType, ActorAiDirtyFlags> actionDirtyFlags = new Dictionary<ActorActionType, ActorAiDirtyFlags>
        {
            { ActorActionType.None, ActorAiDirtyFlags.None },
            { ActorActionType.Wait, ActorAiDirtyFlags.None },
            { ActorActionType.Move, ActorAiDirtyFlags.None },
            { ActorActionType.PickUpItem, ActorAiDirtyFlags.None },
            { ActorActionType.Attack, ActorAiDirtyFlags.None },
            { ActorActionType.Flee, ActorAiDirtyFlags.None },
            { ActorActionType.UseStair, ActorAiDirtyFlags.None },
            { ActorActionType.UseFacility, ActorAiDirtyFlags.None },
            { ActorActionType.Rest, ActorAiDirtyFlags.None },
            { ActorActionType.Equip, ActorAiDirtyFlags.None },
            { ActorActionType.SellItems, ActorAiDirtyFlags.None }
        };

        readonly IReadOnlyDictionary<ActorAiEventType, ActorAiDirtyFlags> eventDirtyFlags = new Dictionary<ActorAiEventType, ActorAiDirtyFlags>
        {
            { ActorAiEventType.HealthBandChanged, ActorAiDirtyFlags.ShortTerm | ActorAiDirtyFlags.MidTerm },
            { ActorAiEventType.EnemyEnteredRange, ActorAiDirtyFlags.ShortTerm },
            { ActorAiEventType.EnteredDungeon, ActorAiDirtyFlags.MidTerm | ActorAiDirtyFlags.ShortTerm },
            { ActorAiEventType.ObjectiveItemCountChanged, ActorAiDirtyFlags.LongTerm | ActorAiDirtyFlags.MidTerm },
            { ActorAiEventType.ObjectiveMonsterDefeated, ActorAiDirtyFlags.LongTerm },
            { ActorAiEventType.DayBoundaryCrossed, ActorAiDirtyFlags.LongTerm | ActorAiDirtyFlags.MidTerm },
            { ActorAiEventType.CurrentActionFailed, ActorAiDirtyFlags.MidTerm | ActorAiDirtyFlags.ShortTerm },
            { ActorAiEventType.CurrentActionCompleted, ActorAiDirtyFlags.ShortTerm }
        };

        public ActorAiDirtyFlags GetDirtyFlags(ActorGoalType goalType)
        {
            return GetDirtyFlags(goalDirtyFlags, goalType);
        }

        public ActorAiDirtyFlags GetDirtyFlags(ActorPlanType planType)
        {
            return GetDirtyFlags(planDirtyFlags, planType);
        }

        public ActorAiDirtyFlags GetDirtyFlags(ActorActionType actionType)
        {
            return GetDirtyFlags(actionDirtyFlags, actionType);
        }

        public ActorAiDirtyFlags GetDirtyFlags(ActorAiEventType eventType)
        {
            return GetDirtyFlags(eventDirtyFlags, eventType);
        }

        public ActorAiDirtyFlags CalculateAdditionalDirtyFlags(ActorGoal goal, ActorPlan plan, ActorAction action)
        {
            var dirtyFlags = ActorAiDirtyFlags.None;

            if (goal != null)
            {
                dirtyFlags |= GetDirtyFlags(goal.Type);
            }

            if (plan != null)
            {
                dirtyFlags |= GetDirtyFlags(plan.Type);
            }

            if (action != null)
            {
                dirtyFlags |= GetDirtyFlags(action.Type);
            }

            return dirtyFlags;
        }

        static ActorAiDirtyFlags GetDirtyFlags<TKey>(IReadOnlyDictionary<TKey, ActorAiDirtyFlags> dirtyFlagsByKey, TKey key)
        {
            if (dirtyFlagsByKey.TryGetValue(key, out var dirtyFlags))
            {
                return dirtyFlags;
            }

            throw new ArgumentOutOfRangeException(nameof(key));
        }
    }
}
