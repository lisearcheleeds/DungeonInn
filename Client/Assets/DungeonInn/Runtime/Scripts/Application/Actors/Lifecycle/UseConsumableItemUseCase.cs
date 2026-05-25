using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Item;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.Actors.Lifecycle
{
    public sealed class UseConsumableItemUseCase
    {
        readonly IMasterRepository masterRepository;
        readonly ActorProcessingCandidateService candidateService;

        [Inject]
        public UseConsumableItemUseCase(
            IMasterRepository masterRepository,
            ActorProcessingCandidateService candidateService)
        {
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
            this.candidateService = candidateService ?? throw new ArgumentNullException(nameof(candidateService));
        }

        public UniTask<bool> ExecuteAsync(Actor actor, int itemId)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            var itemMaster = masterRepository.GetItemMaster(itemId);
            if (!itemMaster.HasTag(ItemTag.Recovery) || itemMaster.ActorEffectMasterId < 1)
            {
                return UniTask.FromResult(false);
            }

            var itemStack = new ItemStack(itemId, 1);
            if (!actor.Inventory.Has(itemStack))
            {
                return UniTask.FromResult(false);
            }

            actor.RemoveItem(itemStack);
            actor.AddActorEffect(masterRepository.GetActorEffectMaster(itemMaster.ActorEffectMasterId));
            candidateService.MarkActorEffectCandidate(actor.Id);
            candidateService.MarkInventoryChanged(actor.Id);
            return UniTask.FromResult(true);
        }
    }
}
