using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Item;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class UseConsumableItemUseCase
    {
        readonly IMasterRepository masterRepository;

        [Inject]
        public UseConsumableItemUseCase(IMasterRepository masterRepository)
        {
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
        }

        public UniTask<bool> ExecuteAsync(Actor actor, int itemId)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            var itemMaster = masterRepository.GetItemMaster(itemId);
            if (itemMaster.Category != ItemCategory.Consumable || itemMaster.ActorEffectMasterId < 1)
            {
                return UniTask.FromResult(false);
            }

            var itemStack = new ItemStack(itemId, 1);
            if (!actor.Inventory.Has(itemStack))
            {
                return UniTask.FromResult(false);
            }

            actor.Inventory.Remove(itemStack);
            actor.AddActorEffect(masterRepository.GetActorEffectMaster(itemMaster.ActorEffectMasterId));
            return UniTask.FromResult(true);
        }
    }
}
