using System.Collections.Generic;
using DungeonInn.Domain.Actor;

namespace DungeonInn.Application.Actors.Phase
{
    public sealed class HardcodedActorActionPhaseMasterRepository : IActorActionPhaseMasterRepository
    {
        readonly IReadOnlyDictionary<ActorActionPhaseKey, IReadOnlyList<ActorActionPhaseDef>> phasesByKey;

        public HardcodedActorActionPhaseMasterRepository()
        {
            phasesByKey = new Dictionary<ActorActionPhaseKey, IReadOnlyList<ActorActionPhaseDef>>
            {
                {
                    new ActorActionPhaseKey(ActorActionType.Attack, null),
                    new[]
                    {
                        new ActorActionPhaseDef(ActorActionPhaseName.WindUp, 0.2f),
                        new ActorActionPhaseDef(ActorActionPhaseName.Effect, 0f),
                        new ActorActionPhaseDef(ActorActionPhaseName.Recovery, 0.8f)
                    }
                }
            };
        }

        public bool TryGetPhaseSequence(
            ActorActionPhaseKey key,
            out IReadOnlyList<ActorActionPhaseDef> phases)
        {
            return phasesByKey.TryGetValue(key, out phases);
        }
    }
}
