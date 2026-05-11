using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Item;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class UseRecoveryItemUseCase
    {
        readonly IMasterRepository masterRepository;
        readonly UseConsumableItemUseCase useConsumableItemUseCase;
        readonly IEventPublisher eventBus;

        [Inject]
        public UseRecoveryItemUseCase(
            IMasterRepository masterRepository,
            UseConsumableItemUseCase useConsumableItemUseCase,
            IEventPublisher eventBus)
        {
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
            this.useConsumableItemUseCase = useConsumableItemUseCase ?? throw new ArgumentNullException(nameof(useConsumableItemUseCase));
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public async UniTask ExecuteAsync(IGameWorldState worldState)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            foreach (var actor in worldState.Actors)
            {
                if (actor.Behavior is not AdventurerBehavior behavior ||
                    behavior.LifecycleState != AdventurerLifecycleState.Exploring)
                {
                    continue;
                }

                if (GameConstants.AdventurerReturnLowHpRatio < actor.Hp / (float)actor.Params.MaxHp)
                {
                    continue;
                }

                var itemId = FindRecoveryItemId(actor);
                if (itemId < 1)
                {
                    continue;
                }

                var actorEffectId = masterRepository.GetItemMaster(itemId).ActorEffectMasterId;
                if (actor.HasActorEffect(actorEffectId))
                {
                    continue;
                }

                var used = await useConsumableItemUseCase.ExecuteAsync(actor, itemId);
                if (used)
                {
                    eventBus.Publish(new ActorAiDecisionRecorded(
                        actor.Id,
                        AiDecisionType.UseRecoveryItem,
                        AiDecisionReasonType.LowHpWithRecoveryItem,
                        default,
                        default,
                        currentHp: actor.Hp,
                        maxHp: actor.Params.MaxHp,
                        selectedFloor: 0,
                        score: 0));
                }
            }
        }

        int FindRecoveryItemId(Actor actor)
        {
            foreach (var kvp in actor.Inventory.ItemCounts)
            {
                if (kvp.Value < 1)
                {
                    continue;
                }

                var itemMaster = masterRepository.GetItemMaster(kvp.Key);
                if (itemMaster.Category != ItemCategory.Consumable || itemMaster.ActorEffectMasterId < 1)
                {
                    continue;
                }

                var actorEffectMaster = masterRepository.GetActorEffectMaster(itemMaster.ActorEffectMasterId);
                if (HasHealHpOverTime(actorEffectMaster))
                {
                    return itemMaster.Id;
                }
            }

            return 0;
        }

        static bool HasHealHpOverTime(ActorEffectMaster actorEffectMaster)
        {
            foreach (var spec in actorEffectMaster.StatusEffectSpecs)
            {
                if (spec.Type == StatusEffectType.HealHpOverTime)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
