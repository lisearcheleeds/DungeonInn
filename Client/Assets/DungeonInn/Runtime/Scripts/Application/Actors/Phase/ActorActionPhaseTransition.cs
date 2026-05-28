using System;

namespace DungeonInn.Application.Actors.Phase
{
    public readonly struct ActorActionPhaseTransition
    {
        public Guid ActorId { get; }
        public ActorActionPhaseKey Key { get; }
        public ActorActionPhaseDef PhaseDef { get; }
        public bool IsFirstTick { get; }

        public ActorActionPhaseTransition(
            Guid actorId,
            ActorActionPhaseKey key,
            ActorActionPhaseDef phaseDef,
            bool isFirstTick)
        {
            ActorId = actorId;
            Key = key;
            PhaseDef = phaseDef;
            IsFirstTick = isFirstTick;
        }
    }
}
