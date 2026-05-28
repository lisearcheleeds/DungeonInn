using System;
using System.Collections.Generic;
using DungeonInn.Domain.Actor;

namespace DungeonInn.Application.Actors.Lifecycle
{
    public sealed class RecoveryItemSelectionPolicy
    {
        public RecoveryItemSelectionPolicy()
        {
        }

        public int SelectItemId(Actor actor, IReadOnlyList<RecoveryItemCandidate> candidates)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            if (candidates == null || candidates.Count == 0)
            {
                return 0;
            }

            var missingHp = actor.Params.MaxHp - actor.Hp;
            var selectedItemId = 0;
            var selectedHealAmount = 0;
            var selectedWaste = int.MaxValue;

            foreach (var candidate in candidates)
            {
                var waste = Math.Max(0, candidate.HealAmount - missingHp);
                if (waste < selectedWaste ||
                    (waste == selectedWaste && candidate.HealAmount < selectedHealAmount))
                {
                    selectedItemId = candidate.ItemId;
                    selectedHealAmount = candidate.HealAmount;
                    selectedWaste = waste;
                }
            }

            return selectedItemId;
        }
    }
}
