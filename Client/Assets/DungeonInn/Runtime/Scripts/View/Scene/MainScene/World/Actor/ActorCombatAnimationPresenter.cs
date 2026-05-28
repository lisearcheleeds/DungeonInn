using System;
using System.Collections.Generic;
using DungeonInn.Application.Actors.Phase;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using R3;
using VContainer;
using VContainer.Unity;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class ActorCombatAnimationPresenter : IInitializable, IDisposable
    {
        readonly IEventSubscriber eventSubscriber;
        readonly Dictionary<Guid, ActorAnimationState> overrides = new();
        DisposableBag bag;

        [Inject]
        public ActorCombatAnimationPresenter(IEventSubscriber eventSubscriber)
        {
            this.eventSubscriber = eventSubscriber ?? throw new ArgumentNullException(nameof(eventSubscriber));
        }

        public void Initialize()
        {
            eventSubscriber.OnEvent<CombatAttackOccurred>()
                .Subscribe(OnCombatAttackOccurred)
                .AddTo(ref bag);

            eventSubscriber.OnEvent<ProjectileHit>()
                .Subscribe(OnProjectileHit)
                .AddTo(ref bag);

            eventSubscriber.OnEvent<AreaEffectHit>()
                .Subscribe(OnAreaEffectHit)
                .AddTo(ref bag);

            eventSubscriber.OnEvent<ActorDefeated>()
                .Subscribe(OnActorDefeated)
                .AddTo(ref bag);

            eventSubscriber.OnEvent<CombatEncounterEnded>()
                .Subscribe(OnCombatEncounterEnded)
                .AddTo(ref bag);

            eventSubscriber.OnEvent<ActorActionPhaseStartedEvent>()
                .Subscribe(OnActorActionPhaseStarted)
                .AddTo(ref bag);

            eventSubscriber.OnEvent<ActorActionSequenceCompletedEvent>()
                .Subscribe(OnActorActionSequenceCompleted)
                .AddTo(ref bag);
        }

        public bool TryGetOverride(Guid actorId, out ActorAnimationState state)
        {
            return overrides.TryGetValue(actorId, out state);
        }

        public void ClearDamageOverride(Guid actorId)
        {
            if (overrides.TryGetValue(actorId, out var state) && state == ActorAnimationState.Damage)
            {
                overrides.Remove(actorId);
            }
        }

        public void RemoveActor(Guid actorId)
        {
            overrides.Remove(actorId);
        }

        public void Dispose()
        {
            bag.Dispose();
        }

        void OnCombatAttackOccurred(CombatAttackOccurred gameEvent)
        {
            SetOverrideUnlessDead(gameEvent.AttackerActorId, ActorAnimationState.Attack);
            SetOverrideUnlessDead(gameEvent.TargetActorId, ActorAnimationState.Damage);
        }

        void OnProjectileHit(ProjectileHit gameEvent)
        {
            SetOverrideUnlessDead(gameEvent.TargetActorId, ActorAnimationState.Damage);
        }

        void OnAreaEffectHit(AreaEffectHit gameEvent)
        {
            SetOverrideUnlessDead(gameEvent.TargetActorId, ActorAnimationState.Damage);
        }

        void OnActorDefeated(ActorDefeated gameEvent)
        {
            overrides[gameEvent.ActorId] = ActorAnimationState.Dead;
        }

        void OnCombatEncounterEnded(CombatEncounterEnded gameEvent)
        {
            if (overrides.TryGetValue(gameEvent.ActorId, out var state) &&
                state != ActorAnimationState.Dead)
            {
                overrides.Remove(gameEvent.ActorId);
            }
        }

        void OnActorActionPhaseStarted(ActorActionPhaseStartedEvent gameEvent)
        {
            SetOverrideUnlessDead(gameEvent.ActorId, ResolvePhaseAnimationState(gameEvent.PhaseName));
        }

        void OnActorActionSequenceCompleted(ActorActionSequenceCompletedEvent gameEvent)
        {
            ClearOverrideUnlessDead(gameEvent.ActorId);
        }

        void SetOverrideUnlessDead(Guid actorId, ActorAnimationState state)
        {
            if (overrides.TryGetValue(actorId, out var currentState) &&
                currentState == ActorAnimationState.Dead)
            {
                return;
            }

            overrides[actorId] = state;
        }

        void ClearOverrideUnlessDead(Guid actorId)
        {
            if (overrides.TryGetValue(actorId, out var currentState) &&
                currentState != ActorAnimationState.Dead)
            {
                overrides.Remove(actorId);
            }
        }

        static ActorAnimationState ResolvePhaseAnimationState(ActorActionPhaseName phaseName)
        {
            switch (phaseName)
            {
                case ActorActionPhaseName.Casting:
                    return ActorAnimationState.Casting;
                case ActorActionPhaseName.WindUp:
                    return ActorAnimationState.WindUp;
                case ActorActionPhaseName.Effect:
                    return ActorAnimationState.Attack;
                case ActorActionPhaseName.Recovery:
                case ActorActionPhaseName.Stagger:
                    return ActorAnimationState.Idle;
                default:
                    return ActorAnimationState.Idle;
            }
        }
    }
}
