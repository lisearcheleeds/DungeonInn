using System;
using System.Collections.Generic;
using VContainer;

namespace DungeonInn.Application.Actors.Phase
{
    public sealed class ActorActionPhaseStateStore : IActorActionPhaseStateStore
    {
        readonly IActorActionPhaseMasterRepository phaseMasterRepository;
        readonly Dictionary<Guid, ActorActionPhaseRuntimeState> statesByActorId = new();
        readonly HashSet<Guid> unreportedStartedActorIds = new();
        readonly Dictionary<Guid, List<ActorActionPhaseDef>> enteredPhaseDefsByActorId = new();
        readonly Dictionary<Guid, ActorActionPhaseKey> completedPhaseKeysByActorId = new();

        [Inject]
        public ActorActionPhaseStateStore(IActorActionPhaseMasterRepository phaseMasterRepository)
        {
            this.phaseMasterRepository = phaseMasterRepository
                ?? throw new ArgumentNullException(nameof(phaseMasterRepository));
        }

        public bool TryStart(Guid actorId, ActorActionPhaseKey key, float currentTime)
        {
            if (!phaseMasterRepository.TryGetPhaseSequence(key, out var phases) || phases.Count == 0)
            {
                return false;
            }

            statesByActorId[actorId] = new ActorActionPhaseRuntimeState(actorId, key, phases, currentTime);
            unreportedStartedActorIds.Add(actorId);
            return true;
        }

        public bool IsActive(Guid actorId)
        {
            return statesByActorId.ContainsKey(actorId);
        }

        public bool TryGetCurrentPhaseDef(Guid actorId, out ActorActionPhaseDef phaseDef)
        {
            if (statesByActorId.TryGetValue(actorId, out var state))
            {
                phaseDef = state.CurrentPhaseDef;
                return !state.IsCompleted;
            }

            phaseDef = default;
            return false;
        }

        public bool TryGetEnteredPhaseDef(
            Guid actorId,
            ActorActionPhaseName phaseName,
            out ActorActionPhaseDef phaseDef)
        {
            if (enteredPhaseDefsByActorId.TryGetValue(actorId, out var phaseDefs))
            {
                foreach (var candidate in phaseDefs)
                {
                    if (candidate.PhaseName == phaseName)
                    {
                        phaseDef = candidate;
                        return true;
                    }
                }
            }

            phaseDef = default;
            return false;
        }

        public bool TryGetLastCompletedPhaseKey(Guid actorId, out ActorActionPhaseKey key)
        {
            return completedPhaseKeysByActorId.TryGetValue(actorId, out key);
        }

        public ActorActionPhaseTickResult TickAll(float currentTime)
        {
            var transitions = new List<ActorActionPhaseTransition>();
            var completedActorIds = new List<Guid>();
            enteredPhaseDefsByActorId.Clear();
            completedPhaseKeysByActorId.Clear();

            foreach (var state in statesByActorId.Values)
            {
                AddInitialTransitionIfNeeded(state, transitions);
                AdvanceState(currentTime, state, transitions, completedActorIds, completedPhaseKeysByActorId);
            }

            foreach (var actorId in completedActorIds)
            {
                statesByActorId.Remove(actorId);
                unreportedStartedActorIds.Remove(actorId);
            }

            return new ActorActionPhaseTickResult(transitions, completedActorIds);
        }

        public void Interrupt(Guid actorId)
        {
            statesByActorId.Remove(actorId);
            unreportedStartedActorIds.Remove(actorId);
        }

        void AddInitialTransitionIfNeeded(
            ActorActionPhaseRuntimeState state,
            ICollection<ActorActionPhaseTransition> transitions)
        {
            if (!unreportedStartedActorIds.Remove(state.ActorId))
            {
                return;
            }

            transitions.Add(new ActorActionPhaseTransition(
                state.ActorId,
                state.PhaseKey,
                state.CurrentPhaseDef,
                true));
            RecordEnteredPhase(state.ActorId, state.CurrentPhaseDef);
        }

        void AdvanceState(
            float currentTime,
            ActorActionPhaseRuntimeState state,
            ICollection<ActorActionPhaseTransition> transitions,
            ICollection<Guid> completedActorIds,
            IDictionary<Guid, ActorActionPhaseKey> completedPhaseKeys)
        {
            while (!state.IsCompleted)
            {
                var currentPhase = state.CurrentPhaseDef;
                var elapsedSeconds = Math.Max(0f, currentTime - state.PhaseStartTime);
                if (elapsedSeconds < currentPhase.DurationSeconds)
                {
                    return;
                }

                var nextPhaseStartTime = state.PhaseStartTime + currentPhase.DurationSeconds;
                state.AdvancePhase(nextPhaseStartTime);
                if (state.IsCompleted)
                {
                    completedActorIds.Add(state.ActorId);
                    completedPhaseKeys[state.ActorId] = state.PhaseKey;
                    return;
                }

                transitions.Add(new ActorActionPhaseTransition(
                    state.ActorId,
                    state.PhaseKey,
                    state.CurrentPhaseDef,
                    true));
                RecordEnteredPhase(state.ActorId, state.CurrentPhaseDef);
            }
        }

        void RecordEnteredPhase(Guid actorId, ActorActionPhaseDef phaseDef)
        {
            if (!enteredPhaseDefsByActorId.TryGetValue(actorId, out var phaseDefs))
            {
                phaseDefs = new List<ActorActionPhaseDef>();
                enteredPhaseDefsByActorId[actorId] = phaseDefs;
            }

            phaseDefs.Add(phaseDef);
        }
    }
}
