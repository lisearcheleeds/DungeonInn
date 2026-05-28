using System;
using System.Collections.Generic;
using DungeonInn.Domain.Actor;
using VContainer;

namespace DungeonInn.Application.Actors.Lifecycle
{
    public sealed class RecoveryEffectEstimator
    {
        [Inject]
        public RecoveryEffectEstimator()
        {
        }

        public int EstimateHpAfterRecovery(Actor actor, IReadOnlyList<RecoveryItemCandidate> candidates)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            var maxHealAmount = 0;
            if (candidates != null)
            {
                foreach (var candidate in candidates)
                {
                    maxHealAmount = Math.Max(maxHealAmount, candidate.HealAmount);
                }
            }

            return Math.Min(actor.Params.MaxHp, actor.Hp + maxHealAmount);
        }
    }
}
