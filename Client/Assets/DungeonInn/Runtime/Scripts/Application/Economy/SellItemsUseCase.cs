using DungeonInn.Application.World;
using System;
using System.Collections.Generic;
using DungeonInn.Application.Actors.Lifecycle;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Commerce;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.Economy
{
    public sealed class SellItemsUseCase
    {
        readonly IItemMasterRepository masterRepository;
        readonly IEventPublisher eventBus;
        readonly IGameClock gameClock;
        readonly PricePolicy pricePolicy = new();
        readonly ExchangeExecutor exchangeExecutor = new();
        readonly List<ItemStack> sellBuffer = new();
        readonly ActorProcessingCandidateService candidateService;
        readonly List<Guid> actorIdBuffer = new();
        readonly Dictionary<ItemCategory, Facility> saleFacilityByCategory = new();

        [Inject]
        public SellItemsUseCase(
            IItemMasterRepository masterRepository,
            IEventPublisher eventBus,
            IGameClock gameClock,
            ActorProcessingCandidateService candidateService)
        {
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            this.gameClock = gameClock ?? throw new ArgumentNullException(nameof(gameClock));
            this.candidateService = candidateService ?? throw new ArgumentNullException(nameof(candidateService));
        }

        public void Execute(IGameWorldState worldState)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            candidateService.CollectSaleCandidates(actorIdBuffer);
            foreach (var actorId in actorIdBuffer)
            {
                var actor = worldState.FindActor(actorId);
                if (actor == null)
                {
                    candidateService.RemoveActor(actorId);
                    continue;
                }

                if (actor.Behavior is not AdventurerBehavior behavior)
                {
                    candidateService.RemoveActor(actor.Id);
                    continue;
                }

                if (behavior.LifecycleState != AdventurerLifecycleState.Returning &&
                    behavior.LifecycleState != AdventurerLifecycleState.Recovering &&
                    behavior.LifecycleState != AdventurerLifecycleState.WaitingForInn)
                {
                    candidateService.ClearSaleCandidate(actor.Id);
                    continue;
                }

                SellItems(worldState.Guild, actor);
                candidateService.ClearSaleCandidate(actor.Id);
            }
        }

        void SellItems(AdventurerGuild guild, Actor actor)
        {
            sellBuffer.Clear();

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

                sellBuffer.Add(new ItemStack(itemId, count));
            }

            foreach (var stack in sellBuffer)
            {
                var itemMaster = masterRepository.GetItemMaster(stack.ItemId);
                if (!TryFindSaleFacility(guild, itemMaster.Category, out var facility))
                {
                    continue;
                }

                var price = pricePolicy.CalculatePurchasePrice(stack, masterRepository.ItemMasters);
                if (!facility.Inventory.Has(price) ||
                    !facility.Inventory.CanAdd(stack) ||
                    !actor.Inventory.CanAdd(price))
                {
                    continue;
                }

                var transaction = exchangeExecutor.Execute(
                    actor,
                    facility,
                    stack,
                    price,
                    gameClock.CurrentScheduleTick);
                guild.RecordTransaction(transaction);

                eventBus.Publish(new ItemSold(actor.Id, stack, price.Count, actor.Inventory.Gold));
                candidateService.MarkInventoryChanged(actor.Id);
            }
        }

        bool TryFindSaleFacility(AdventurerGuild guild, ItemCategory itemCategory, out Facility facility)
        {
            if (saleFacilityByCategory.TryGetValue(itemCategory, out facility))
            {
                return true;
            }

            if (!TryGetSaleFacilityType(itemCategory, out var facilityType))
            {
                facility = null;
                return false;
            }

            foreach (var candidate in guild.Facilities)
            {
                if (candidate.Type == facilityType)
                {
                    facility = candidate;
                    saleFacilityByCategory[itemCategory] = facility;
                    return true;
                }
            }

            facility = null;
            return false;
        }

        static bool TryGetSaleFacilityType(ItemCategory itemCategory, out FacilityType facilityType)
        {
            switch (itemCategory)
            {
                case ItemCategory.Material:
                    facilityType = FacilityType.GeneralStore;
                    return true;
                case ItemCategory.Equipment:
                    facilityType = FacilityType.EquipmentShop;
                    return true;
                default:
                    facilityType = default;
                    return false;
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
