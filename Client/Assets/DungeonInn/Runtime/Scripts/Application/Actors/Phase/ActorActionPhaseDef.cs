using System;

namespace DungeonInn.Application.Actors.Phase
{
    public readonly struct ActorActionPhaseDef
    {
        public ActorActionPhaseName PhaseName { get; }
        public float DurationSeconds { get; }

        public ActorActionPhaseDef(ActorActionPhaseName phaseName, float durationSeconds)
        {
            PhaseName = phaseName;
            DurationSeconds = Math.Max(0f, durationSeconds);
        }
    }
}
