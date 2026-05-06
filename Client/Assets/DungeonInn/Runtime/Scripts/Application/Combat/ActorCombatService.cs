using System;
using System.Collections.Generic;

namespace DungeonInn.Application.Combat
{
    public sealed class ActorCombatService : IActorCombatService
    {
        readonly Dictionary<Guid, ActorCombatState> states = new();

        public ActorCombatState GetOrCreateCombatState(Guid actorId)
        {
            if (!states.TryGetValue(actorId, out var state))
            {
                state = new ActorCombatState();
                states[actorId] = state;
            }

            return state;
        }

        public bool HasTarget(Guid actorId)
        {
            return states.TryGetValue(actorId, out var state) && state.HasTarget;
        }

        public void ClearTarget(Guid actorId)
        {
            if (states.TryGetValue(actorId, out var state))
            {
                state.ClearTarget();
            }
        }

        public void ClearTargetsReferencing(Guid targetActorId)
        {
            foreach (var state in states.Values)
            {
                if (state.TargetActorId.HasValue && state.TargetActorId.Value.Equals(targetActorId))
                {
                    state.ClearTarget();
                }
            }
        }

        public void RemoveState(Guid actorId)
        {
            states.Remove(actorId);
        }
    }
}
