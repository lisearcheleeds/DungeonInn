using System;
using System.Collections.Generic;

namespace DungeonInn.Application.Combat
{
    public sealed class ActorCombatService : IActorCombatService
    {
        readonly Dictionary<Guid, ActorCombatState> states = new();
        readonly Dictionary<Guid, HashSet<Guid>> targetedBy = new();

        public ActorCombatState GetOrCreateCombatState(Guid actorId)
        {
            if (!states.TryGetValue(actorId, out var state))
            {
                state = new ActorCombatState();
                states[actorId] = state;
            }

            return state;
        }

        public void SetTarget(Guid actorId, Guid targetId)
        {
            var state = GetOrCreateCombatState(actorId);

            if (state.TargetActorId.HasValue && state.TargetActorId.Value.Equals(targetId))
            {
                return;
            }

            if (state.TargetActorId.HasValue)
            {
                RemoveFromTargetedBy(state.TargetActorId.Value, actorId);
            }

            state.SetTarget(targetId);

            if (!targetedBy.TryGetValue(targetId, out var attackers))
            {
                attackers = new HashSet<Guid>();
                targetedBy[targetId] = attackers;
            }

            attackers.Add(actorId);
        }

        public bool HasTarget(Guid actorId)
        {
            return states.TryGetValue(actorId, out var state) && state.HasTarget;
        }

        public void ClearTarget(Guid actorId)
        {
            if (!states.TryGetValue(actorId, out var state))
            {
                return;
            }

            if (state.TargetActorId.HasValue)
            {
                RemoveFromTargetedBy(state.TargetActorId.Value, actorId);
            }

            state.ClearTarget();
        }

        public void ClearTargetsReferencing(Guid targetActorId)
        {
            if (!targetedBy.TryGetValue(targetActorId, out var attackers))
            {
                return;
            }

            foreach (var attackerId in attackers)
            {
                if (states.TryGetValue(attackerId, out var state))
                {
                    state.ClearTarget();
                }
            }

            targetedBy.Remove(targetActorId);
        }

        public void RemoveState(Guid actorId)
        {
            if (states.TryGetValue(actorId, out var state) && state.TargetActorId.HasValue)
            {
                RemoveFromTargetedBy(state.TargetActorId.Value, actorId);
            }

            states.Remove(actorId);
            targetedBy.Remove(actorId);
        }

        public void MarkCombatParticipation(Guid actorId)
        {
            GetOrCreateCombatState(actorId).MarkCombatParticipation();
        }

        public bool HasParticipatedInCombat(Guid actorId)
        {
            return states.TryGetValue(actorId, out var state) && state.HasParticipatedInCombat;
        }

        public void ClearCombatHistory(Guid actorId)
        {
            if (states.TryGetValue(actorId, out var state))
            {
                state.ClearCombatHistory();
            }
        }

        public bool IsTargetedByAny(Guid actorId)
        {
            return targetedBy.TryGetValue(actorId, out var attackers) && 0 < attackers.Count;
        }

        public IReadOnlyCollection<Guid> GetAttackers(Guid targetId)
        {
            if (targetedBy.TryGetValue(targetId, out var attackers))
            {
                return attackers;
            }

            return Array.Empty<Guid>();
        }

        void RemoveFromTargetedBy(Guid targetId, Guid actorId)
        {
            if (targetedBy.TryGetValue(targetId, out var attackers))
            {
                attackers.Remove(actorId);
            }
        }
    }
}
