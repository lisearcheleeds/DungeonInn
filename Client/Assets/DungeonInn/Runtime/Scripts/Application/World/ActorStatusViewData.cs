using System;
using System.Collections.Generic;

namespace DungeonInn.Application.World
{
    public readonly struct ActorStatusViewData
    {
        public ActorStatusViewData(
            Guid actorId,
            float hpRatio,
            IReadOnlyList<ActorEffectIconViewData> activeEffects)
        {
            if (actorId == Guid.Empty)
            {
                throw new ArgumentException("Actor id is required.", nameof(actorId));
            }

            ActorId = actorId;
            HpRatio = hpRatio;
            ActiveEffects = activeEffects ?? throw new ArgumentNullException(nameof(activeEffects));
        }

        public Guid ActorId { get; }
        public float HpRatio { get; }
        public IReadOnlyList<ActorEffectIconViewData> ActiveEffects { get; }
    }
}
