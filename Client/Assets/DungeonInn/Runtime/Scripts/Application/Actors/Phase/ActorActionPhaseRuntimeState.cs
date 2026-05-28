using System;
using System.Collections.Generic;

namespace DungeonInn.Application.Actors.Phase
{
    public sealed class ActorActionPhaseRuntimeState
    {
        public Guid ActorId { get; }
        public ActorActionPhaseKey PhaseKey { get; }
        public IReadOnlyList<ActorActionPhaseDef> Phases { get; }
        public int CurrentPhaseIndex { get; private set; }
        public float PhaseStartTime { get; private set; }
        public bool IsCompleted => Phases.Count <= CurrentPhaseIndex;

        public ActorActionPhaseDef CurrentPhaseDef
        {
            get
            {
                if (IsCompleted)
                {
                    return default;
                }

                return Phases[CurrentPhaseIndex];
            }
        }

        public ActorActionPhaseRuntimeState(
            Guid actorId,
            ActorActionPhaseKey phaseKey,
            IReadOnlyList<ActorActionPhaseDef> phases,
            float phaseStartTime)
        {
            ActorId = actorId;
            PhaseKey = phaseKey;
            Phases = phases ?? throw new ArgumentNullException(nameof(phases));
            CurrentPhaseIndex = 0;
            PhaseStartTime = phaseStartTime;
        }

        public void AdvancePhase(float phaseStartTime)
        {
            CurrentPhaseIndex++;
            PhaseStartTime = phaseStartTime;
        }
    }
}
