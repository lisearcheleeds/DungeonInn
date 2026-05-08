using System;

namespace DungeonInn.Application.Combat
{
    public interface IActorCombatService
    {
        ActorCombatState GetOrCreateCombatState(Guid actorId);
        void SetTarget(Guid actorId, Guid targetId);
        bool HasTarget(Guid actorId);
        void ClearTarget(Guid actorId);
        void ClearTargetsReferencing(Guid targetActorId);
        void RemoveState(Guid actorId);
        void MarkCombatParticipation(Guid actorId);
        bool HasParticipatedInCombat(Guid actorId);
        void ClearCombatHistory(Guid actorId);
        bool IsTargetedByAny(Guid actorId);
    }
}
