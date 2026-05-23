using System;
using System.Collections.Generic;
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
        }

        public bool TryGetOverride(Guid actorId, out ActorAnimationState state)
        {
            return overrides.TryGetValue(actorId, out state);
        }

        public void ClearHitOverride(Guid actorId)
        {
            if (overrides.TryGetValue(actorId, out var state) && state == ActorAnimationState.Hit)
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
            SetOverrideUnlessDead(gameEvent.AttackerActorId, ActorAnimationState.Combat);
            SetOverrideUnlessDead(gameEvent.TargetActorId, ActorAnimationState.Hit);
        }

        void OnProjectileHit(ProjectileHit gameEvent)
        {
            SetOverrideUnlessDead(gameEvent.TargetActorId, ActorAnimationState.Hit);
        }

        void OnAreaEffectHit(AreaEffectHit gameEvent)
        {
            SetOverrideUnlessDead(gameEvent.TargetActorId, ActorAnimationState.Hit);
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

        void SetOverrideUnlessDead(Guid actorId, ActorAnimationState state)
        {
            if (overrides.TryGetValue(actorId, out var currentState) &&
                currentState == ActorAnimationState.Dead)
            {
                return;
            }

            overrides[actorId] = state;
        }
    }
}
