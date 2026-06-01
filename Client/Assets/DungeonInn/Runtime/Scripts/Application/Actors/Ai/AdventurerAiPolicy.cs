using System;
using System.Collections.Generic;
using R3;
using VContainer;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.Facilities;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.Actors.Ai
{
    public sealed class AdventurerAiPolicy : IActorAiPolicy, IDisposable
    {
        readonly Dictionary<Guid, int> lastKnownExplorationRoomArrivalCountByActor = new();
        readonly Dictionary<Guid, int> lastKnownInventoryCountByActor = new();
        readonly FacilityNeedSelector facilityNeedSelector;
        DisposableBag bag;

        [Inject]
        public AdventurerAiPolicy(
            IEventSubscriber eventSubscriber,
            FacilityNeedSelector facilityNeedSelector)
        {
            if (eventSubscriber == null)
            {
                throw new ArgumentNullException(nameof(eventSubscriber));
            }

            this.facilityNeedSelector = facilityNeedSelector;

            eventSubscriber.OnEvent<ActorDefeated>()
                .Subscribe(gameEvent => { RemoveState(gameEvent.ActorId); })
                .AddTo(ref bag);
            eventSubscriber.OnEvent<ActorDeparted>()
                .Subscribe(gameEvent => { RemoveState(gameEvent.ActorId); })
                .AddTo(ref bag);
        }

        public bool CanHandle(Actor actor)
        {
            return actor.Behavior is AdventurerBehavior;
        }

        public ActorAiDecision EvaluateLongTerm(ActorAiContext context)
        {
            if (context.Actor.CurrentGoal.Type != ActorGoalType.None && !context.Actor.CurrentGoal.IsCompleted())
            {
                return ActorAiDecision.None();
            }

            return new ActorAiDecision(
                new ActorGoal(ActorGoalType.LevelUp, 0, 0, 0),
                null,
                null);
        }

        public ActorAiDecision EvaluateMidTerm(ActorAiContext context)
        {
            if (context.Actor.CurrentPlan.Type != ActorPlanType.None)
            {
                return ActorAiDecision.None();
            }

            var actor = context.Actor;
            if (actor.Behavior is AdventurerBehavior behavior &&
                actor.Position.LayerId.Equals(MapLayerId.Ground) &&
                behavior.LifecycleState == AdventurerLifecycleState.Recovering)
            {
                if (context.WorldState != null &&
                    facilityNeedSelector != null &&
                    facilityNeedSelector.TrySelectFacility(context.WorldState.Guild, actor, out var facility))
                {
                    return new ActorAiDecision(
                        null,
                        ActorPlan.UseFacility(facility.Id),
                        null);
                }

                if (actor.Hp < actor.Params.MaxHp)
                {
                    return ActorAiDecision.None();
                }
            }

            if (actor.Behavior is AdventurerBehavior waitingBehavior &&
                actor.Position.LayerId.Equals(MapLayerId.Ground) &&
                waitingBehavior.LifecycleState == AdventurerLifecycleState.WaitingForInn)
            {
                return ActorAiDecision.None();
            }

            return new ActorAiDecision(
                null,
                new ActorPlan(ActorPlanType.Prepare, 0, 0),
                null);
        }

        public ActorAiDecision EvaluateShortTerm(ActorAiContext context)
        {
            var actor = context.Actor;
            var behavior = actor.RequireBehavior<AdventurerBehavior>();
            var inventoryCount = CountInventoryItems(actor);

            if (!lastKnownInventoryCountByActor.TryGetValue(actor.Id, out var lastInventoryCount))
            {
                lastInventoryCount = inventoryCount;
            }

            lastKnownExplorationRoomArrivalCountByActor.TryGetValue(actor.Id, out var lastRoomArrivalCount);
            lastKnownInventoryCountByActor[actor.Id] = inventoryCount;
            lastKnownExplorationRoomArrivalCountByActor[actor.Id] = behavior.ExplorationRoomArrivalCount;

            if (lastRoomArrivalCount < behavior.ExplorationRoomArrivalCount)
            {
                return new ActorAiDecision(
                    null,
                    null,
                    null,
                    ActorAiCooldownSeconds.PostRoomArrival);
            }

            if (lastInventoryCount < inventoryCount)
            {
                return new ActorAiDecision(
                    null,
                    null,
                    null,
                    ActorAiCooldownSeconds.PostPickUpItem);
            }

            if (context.Actor.CurrentAction.State == ActorActionState.Running)
            {
                return ActorAiDecision.None();
            }

            if (context.Actor.CurrentPlan.Type == ActorPlanType.UseFacility)
            {
                return ActorAiDecision.None();
            }

            return new ActorAiDecision(null, null, ActorAction.Wait());
        }

        static int CountInventoryItems(Actor actor)
        {
            var count = 0;
            foreach (var kvp in actor.Inventory.ItemCounts)
            {
                count += kvp.Value;
            }

            return count;
        }

        void RemoveState(Guid actorId)
        {
            lastKnownExplorationRoomArrivalCountByActor.Remove(actorId);
            lastKnownInventoryCountByActor.Remove(actorId);
        }

        public void Dispose()
        {
            bag.Dispose();
        }
    }
}
