using System;

namespace DungeonInn.Application.Combat
{
    public interface IActorCombatService
    {
        ActorCombatState GetOrCreateCombatState(Guid actorId);
        bool HasTarget(Guid actorId);
        void ClearTarget(Guid actorId);
        void ClearTargetsReferencing(Guid targetActorId);
        void RemoveState(Guid actorId);
    }
}
