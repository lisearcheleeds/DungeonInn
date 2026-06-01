using System;
using System.Collections.Generic;
using R3;
using VContainer;
using DungeonInn.Application.Actors.Phase;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Domain.Actor;

namespace DungeonInn.Application.Actors.Ai
{
    public sealed class ActorDecisionScheduler : IDisposable
    {
        readonly Dictionary<Guid, ActorAiRuntimeState> states = new();
        readonly ActorAiEventDirtyMapper dirtyMapper = new();
        DisposableBag bag;

        [Inject]
        public ActorDecisionScheduler(IEventSubscriber eventSubscriber)
        {
            if (eventSubscriber == null)
            {
                throw new ArgumentNullException(nameof(eventSubscriber));
            }

            eventSubscriber.OnEvent<ActorDefeated>()
                .Subscribe(gameEvent => { RemoveState(gameEvent.ActorId); })
                .AddTo(ref bag);
            eventSubscriber.OnEvent<ActorDeparted>()
                .Subscribe(gameEvent => { RemoveState(gameEvent.ActorId); })
                .AddTo(ref bag);
            eventSubscriber.OnEvent<ExplorationRoomArrived>()
                .Subscribe(gameEvent => { MarkEvent(gameEvent.ActorId, ActorAiEventType.CurrentActionCompleted); })
                .AddTo(ref bag);
            eventSubscriber.OnEvent<ItemPickedUp>()
                .Subscribe(gameEvent => { MarkEvent(gameEvent.ActorId, ActorAiEventType.CurrentActionCompleted); })
                .AddTo(ref bag);
            eventSubscriber.OnEvent<ActorActionSequenceCompletedEvent>()
                .Subscribe(gameEvent => { MarkEvent(gameEvent.ActorId, ActorAiEventType.CurrentActionCompleted); })
                .AddTo(ref bag);
            eventSubscriber.OnEvent<ActorExitedDungeon>()
                .Subscribe(gameEvent => { MarkDirty(gameEvent.ActorId, ActorAiDirtyFlags.MidTerm | ActorAiDirtyFlags.ShortTerm); })
                .AddTo(ref bag);
            eventSubscriber.OnEvent<ActorFullyRecovered>()
                .Subscribe(gameEvent => { MarkDirty(gameEvent.ActorId, ActorAiDirtyFlags.MidTerm | ActorAiDirtyFlags.ShortTerm); })
                .AddTo(ref bag);
        }

        public ActorAiRuntimeState GetOrCreateState(Guid actorId)
        {
            if (states.TryGetValue(actorId, out var state))
            {
                return state;
            }

            state = new ActorAiRuntimeState(actorId);
            states.Add(actorId, state);
            return state;
        }

        public void MarkDirty(Guid actorId, ActorAiDirtyFlags dirtyFlags)
        {
            GetOrCreateState(actorId).MarkDirty(dirtyFlags);
        }

        public void MarkEvent(Guid actorId, ActorAiEventType eventType)
        {
            MarkDirty(actorId, dirtyMapper.Map(eventType));
        }

        public void RemoveState(Guid actorId)
        {
            states.Remove(actorId);
        }

        public bool TryGetEvaluationTarget(
            IEnumerable<Actor> actors,
            float currentTimeSeconds,
            int evaluationFrameId,
            out Actor actor,
            out ActorAiRuntimeState runtimeState)
        {
            foreach (var candidate in actors)
            {
                var state = GetOrCreateState(candidate.Id);
                if (!state.CanEvaluate(currentTimeSeconds, evaluationFrameId))
                {
                    continue;
                }

                actor = candidate;
                runtimeState = state;
                return true;
            }

            actor = null;
            runtimeState = null;
            return false;
        }

        public void Dispose()
        {
            bag.Dispose();
        }
    }
}
