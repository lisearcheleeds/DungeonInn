using System;

namespace DungeonInn.Application.Actors.Phase
{
    public interface IActorActionPhaseStateStore
    {
        bool TryStart(Guid actorId, ActorActionPhaseKey key, float currentTime);
        bool IsActive(Guid actorId);
        bool TryGetCurrentPhaseDef(Guid actorId, out ActorActionPhaseDef phaseDef);
        bool TryGetEnteredPhaseDef(Guid actorId, ActorActionPhaseName phaseName, out ActorActionPhaseDef phaseDef);
        bool TryGetLastCompletedPhaseKey(Guid actorId, out ActorActionPhaseKey key);
        ActorActionPhaseTickResult TickAll(float currentTime);
        void Interrupt(Guid actorId);
    }
}
