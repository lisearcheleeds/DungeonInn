using System;
using System.Collections.Generic;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Item;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class SellItemsUseCase
    {
        readonly IItemMasterRepository masterRepository;
        readonly IGameEventBus eventBus;

        [Inject]
        public SellItemsUseCase(IItemMasterRepository masterRepository, IGameEventBus eventBus)
        {
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public void Execute(IGameWorldState worldState)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            foreach (var actor in worldState.Actors)
            {
                if (actor.Behavior is not AdventurerBehavior behavior)
                {
                    continue;
                }

                if (behavior.LifecycleState != AdventurerLifecycleState.Returning &&
                    behavior.LifecycleState != AdventurerLifecycleState.Recovering)
                {
                    continue;
                }

                SellItems(actor);
            }
        }

        void SellItems(Actor actor)
        {
            var toSell = new List<ItemStack>();

            foreach (var kvp in actor.Inventory.ItemCounts)
            {
                var itemId = kvp.Key;
                var count = kvp.Value;

                if (itemId == SpecialItemIds.Money)
                {
                    continue;
                }

                if (!masterRepository.ItemMasters.TryGetValue(itemId, out var itemMaster))
                {
                    continue;
                }

                if (!itemMaster.CanTrade)
                {
                    continue;
                }

                if (!IsSellable(actor, itemMaster))
                {
                    continue;
                }

                toSell.Add(new ItemStack(itemId, count));
            }

            foreach (var stack in toSell)
            {
                var itemMaster = masterRepository.GetItemMaster(stack.ItemId);
                var totalPrice = itemMaster.BasePrice * stack.Count;

                actor.Inventory.Remove(stack);
                actor.Inventory.AddGold(totalPrice);

                eventBus.Publish(new ItemSold(actor.Id, stack, totalPrice, actor.Inventory.Gold));
            }
        }

        static bool IsSellable(Actor actor, ItemMaster itemMaster)
        {
            switch (itemMaster.Category)
            {
                case ItemCategory.Material:
                    return true;
                case ItemCategory.Equipment:
                    return !IsEquipped(actor, itemMaster.Id);
                default:
                    return false;
            }
        }

        static bool IsEquipped(Actor actor, int itemId)
        {
            foreach (var equippedMaster in actor.Equipment.EquippedMasters.Values)
            {
                if (equippedMaster.ItemId == itemId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
