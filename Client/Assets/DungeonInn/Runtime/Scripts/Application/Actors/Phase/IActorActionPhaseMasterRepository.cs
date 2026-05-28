using System.Collections.Generic;

namespace DungeonInn.Application.Actors.Phase
{
    public interface IActorActionPhaseMasterRepository
    {
        bool TryGetPhaseSequence(
            ActorActionPhaseKey key,
            out IReadOnlyList<ActorActionPhaseDef> phases);
    }
}
