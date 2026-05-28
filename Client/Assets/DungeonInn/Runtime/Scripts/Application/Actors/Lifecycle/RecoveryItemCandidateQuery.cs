using System;
using System.Collections.Generic;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Item;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.Actors.Lifecycle
{
    public sealed class RecoveryItemCandidateQuery
    {
        readonly IMasterRepository masterRepository;

        [Inject]
        public RecoveryItemCandidateQuery(IMasterRepository masterRepository)
        {
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
        }

        public IReadOnlyList<RecoveryItemCandidate> Execute(Actor actor)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            var candidates = new List<RecoveryItemCandidate>();
            foreach (var kvp in actor.Inventory.ItemCounts)
            {
                if (kvp.Value < 1)
                {
                    continue;
                }

                var itemMaster = masterRepository.GetItemMaster(kvp.Key);
                if (!itemMaster.HasTag(ItemTag.Recovery) || itemMaster.ActorEffectMasterId < 1)
                {
                    continue;
                }

                var healAmount = GetHealAmount(masterRepository.GetActorEffectMaster(itemMaster.ActorEffectMasterId));
                if (healAmount < 1)
                {
                    continue;
                }

                candidates.Add(new RecoveryItemCandidate(itemMaster.Id, itemMaster.ActorEffectMasterId, healAmount));
            }

            return candidates;
        }

        static int GetHealAmount(ActorEffectMaster actorEffectMaster)
        {
            var result = 0;
            foreach (var spec in actorEffectMaster.StatusEffectSpecs)
            {
                if (spec.Type == StatusEffectType.HealHpOverTime)
                {
                    result += spec.Amount;
                }
            }

            return result;
        }
    }
}
