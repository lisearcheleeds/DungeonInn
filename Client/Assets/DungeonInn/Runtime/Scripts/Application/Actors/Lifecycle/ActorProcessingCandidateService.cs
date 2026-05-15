using System;
using System.Collections.Generic;
using R3;
using VContainer;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Domain.Actor;

namespace DungeonInn.Application.Actors.Lifecycle
{
    public sealed class ActorProcessingCandidateService : IDisposable
    {
        readonly HashSet<Guid> itemPickupActorIds = new();
        readonly HashSet<Guid> actorEffectActorIds = new();
        readonly HashSet<Guid> recoveryActorIds = new();
        readonly HashSet<Guid> reservationActorIds = new();
        readonly HashSet<Guid> equipmentActorIds = new();
        readonly HashSet<Guid> saleActorIds = new();
        readonly HashSet<Guid> recoveryItemActorIds = new();
        DisposableBag bag;

        [Inject]
        public ActorProcessingCandidateService(IEventSubscriber eventSubscriber)
        {
            if (eventSubscriber == null)
            {
                throw new ArgumentNullException(nameof(eventSubscriber));
            }

            eventSubscriber.OnEvent<ActorDefeated>()
                .Subscribe(gameEvent => RemoveActor(gameEvent.ActorId))
                .AddTo(ref bag);
            eventSubscriber.OnEvent<ActorDeparted>()
                .Subscribe(gameEvent => RemoveActor(gameEvent.ActorId))
                .AddTo(ref bag);
            eventSubscriber.OnEvent<ItemPickedUp>()
                .Subscribe(gameEvent => MarkInventoryChanged(gameEvent.ActorId))
                .AddTo(ref bag);
            eventSubscriber.OnEvent<ItemSold>()
                .Subscribe(gameEvent => MarkInventoryChanged(gameEvent.ActorId))
                .AddTo(ref bag);
            eventSubscriber.OnEvent<EquipmentChanged>()
                .Subscribe(gameEvent => MarkInventoryChanged(gameEvent.ActorId))
                .AddTo(ref bag);
            eventSubscriber.OnEvent<CombatAttackOccurred>()
                .Subscribe(gameEvent => MarkRecoveryItemCandidate(gameEvent.TargetActorId))
                .AddTo(ref bag);
            eventSubscriber.OnEvent<ActorEnteredDungeon>()
                .Subscribe(gameEvent => MarkItemPickupCandidate(gameEvent.ActorId))
                .AddTo(ref bag);
            eventSubscriber.OnEvent<ActorExitedDungeon>()
                .Subscribe(gameEvent => MarkPostDungeonScheduleCandidates(gameEvent.ActorId))
                .AddTo(ref bag);
            eventSubscriber.OnEvent<ActorStartedReturning>()
                .Subscribe(gameEvent => MarkPostDungeonScheduleCandidates(gameEvent.ActorId))
                .AddTo(ref bag);
            eventSubscriber.OnEvent<ActorWaitingForInn>()
                .Subscribe(gameEvent => MarkInnCandidates(gameEvent.ActorId))
                .AddTo(ref bag);
            eventSubscriber.OnEvent<ActorReservedInn>()
                .Subscribe(gameEvent => MarkInnCandidates(gameEvent.ActorId))
                .AddTo(ref bag);
            eventSubscriber.OnEvent<ActorFullyRecovered>()
                .Subscribe(gameEvent => ClearInnCandidates(gameEvent.ActorId))
                .AddTo(ref bag);
        }

        public bool HasItemPickupCandidates => 0 < itemPickupActorIds.Count;
        public bool HasActorEffectCandidates => 0 < actorEffectActorIds.Count;
        public bool HasRecoveryCandidates => 0 < recoveryActorIds.Count;
        public bool HasReservationCandidates => 0 < reservationActorIds.Count;
        public bool HasEquipmentCandidates => 0 < equipmentActorIds.Count;
        public bool HasSaleCandidates => 0 < saleActorIds.Count;
        public bool HasRecoveryItemCandidates => 0 < recoveryItemActorIds.Count;

        public void SyncActor(Actor actor)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            if (0 < actor.ActorEffects.Count)
            {
                actorEffectActorIds.Add(actor.Id);
            }

            if (actor.Behavior is not AdventurerBehavior behavior)
            {
                return;
            }

            if (behavior.LifecycleState == AdventurerLifecycleState.Exploring)
            {
                itemPickupActorIds.Add(actor.Id);
                recoveryItemActorIds.Add(actor.Id);
            }

            if (behavior.LifecycleState == AdventurerLifecycleState.Returning ||
                behavior.LifecycleState == AdventurerLifecycleState.Recovering ||
                behavior.LifecycleState == AdventurerLifecycleState.WaitingForInn)
            {
                MarkPostDungeonScheduleCandidates(actor.Id);
            }

            if (behavior.LifecycleState == AdventurerLifecycleState.Recovering)
            {
                recoveryActorIds.Add(actor.Id);
                reservationActorIds.Add(actor.Id);
            }

            if (behavior.LifecycleState == AdventurerLifecycleState.WaitingForInn)
            {
                reservationActorIds.Add(actor.Id);
            }
        }

        public void MarkItemPickupCandidate(Guid actorId)
        {
            itemPickupActorIds.Add(actorId);
        }

        public void MarkActorEffectCandidate(Guid actorId)
        {
            actorEffectActorIds.Add(actorId);
        }

        public void MarkRecoveryCandidate(Guid actorId)
        {
            recoveryActorIds.Add(actorId);
            reservationActorIds.Add(actorId);
        }

        public void MarkInnCandidates(Guid actorId)
        {
            recoveryActorIds.Add(actorId);
            reservationActorIds.Add(actorId);
            saleActorIds.Add(actorId);
            equipmentActorIds.Add(actorId);
        }

        public void MarkPostDungeonScheduleCandidates(Guid actorId)
        {
            equipmentActorIds.Add(actorId);
            saleActorIds.Add(actorId);
            recoveryItemActorIds.Add(actorId);
            reservationActorIds.Add(actorId);
        }

        public void MarkInventoryChanged(Guid actorId)
        {
            equipmentActorIds.Add(actorId);
            saleActorIds.Add(actorId);
            recoveryItemActorIds.Add(actorId);
        }

        public void MarkRecoveryItemCandidate(Guid actorId)
        {
            recoveryItemActorIds.Add(actorId);
        }

        public void ClearActorEffectCandidate(Guid actorId)
        {
            actorEffectActorIds.Remove(actorId);
        }

        public void ClearRecoveryCandidate(Guid actorId)
        {
            recoveryActorIds.Remove(actorId);
        }

        public void ClearReservationCandidate(Guid actorId)
        {
            reservationActorIds.Remove(actorId);
        }

        public void ClearEquipmentCandidate(Guid actorId)
        {
            equipmentActorIds.Remove(actorId);
        }

        public void ClearSaleCandidate(Guid actorId)
        {
            saleActorIds.Remove(actorId);
        }

        public void ClearRecoveryItemCandidate(Guid actorId)
        {
            recoveryItemActorIds.Remove(actorId);
        }

        public void CollectItemPickupCandidates(List<Guid> results)
        {
            Collect(itemPickupActorIds, results, false);
        }

        public void CollectActorEffectCandidates(List<Guid> results)
        {
            Collect(actorEffectActorIds, results, false);
        }

        public void CollectRecoveryCandidates(List<Guid> results)
        {
            Collect(recoveryActorIds, results, false);
        }

        public void CollectReservationCandidates(List<Guid> results)
        {
            Collect(reservationActorIds, results, false);
        }

        public void CollectEquipmentCandidates(List<Guid> results)
        {
            Collect(equipmentActorIds, results, false);
        }

        public void CollectSaleCandidates(List<Guid> results)
        {
            Collect(saleActorIds, results, false);
        }

        public void CollectRecoveryItemCandidates(List<Guid> results)
        {
            Collect(recoveryItemActorIds, results, false);
        }

        public void RemoveActor(Guid actorId)
        {
            itemPickupActorIds.Remove(actorId);
            actorEffectActorIds.Remove(actorId);
            recoveryActorIds.Remove(actorId);
            reservationActorIds.Remove(actorId);
            equipmentActorIds.Remove(actorId);
            saleActorIds.Remove(actorId);
            recoveryItemActorIds.Remove(actorId);
        }

        public void Dispose()
        {
            bag.Dispose();
        }

        void ClearInnCandidates(Guid actorId)
        {
            recoveryActorIds.Remove(actorId);
            reservationActorIds.Remove(actorId);
        }

        static void Collect(HashSet<Guid> source, List<Guid> results, bool clear)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            results.Clear();
            foreach (var actorId in source)
            {
                results.Add(actorId);
            }

            if (clear)
            {
                source.Clear();
            }
        }
    }
}
