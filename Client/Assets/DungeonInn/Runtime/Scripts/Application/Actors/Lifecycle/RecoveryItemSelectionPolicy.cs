using System;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Item;
using DungeonInn.Master;

namespace DungeonInn.Application.Actors.Lifecycle
{
    public sealed class RecoveryItemSelectionPolicy
    {
        readonly IMasterRepository masterRepository;

        public RecoveryItemSelectionPolicy(IMasterRepository masterRepository)
        {
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
        }

        public int SelectItemId(Actor actor)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            var missingHp = actor.Params.MaxHp - actor.Hp;
            var selectedItemId = 0;
            var selectedHealAmount = 0;
            var selectedWaste = int.MaxValue;

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

                var waste = Math.Max(0, healAmount - missingHp);
                if (waste < selectedWaste ||
                    (waste == selectedWaste && healAmount < selectedHealAmount))
                {
                    selectedItemId = itemMaster.Id;
                    selectedHealAmount = healAmount;
                    selectedWaste = waste;
                }
            }

            return selectedItemId;
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
