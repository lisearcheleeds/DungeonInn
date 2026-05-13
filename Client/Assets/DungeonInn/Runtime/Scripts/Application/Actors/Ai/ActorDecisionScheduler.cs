using System;
using System.Collections.Generic;
using DungeonInn.Domain.Actor;

namespace DungeonInn.Application.Actors.Ai
{
    public sealed class ActorDecisionScheduler
    {
        readonly Dictionary<Guid, ActorAiRuntimeState> states = new();
        readonly ActorAiEventDirtyMapper dirtyMapper = new();

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
    }
}
