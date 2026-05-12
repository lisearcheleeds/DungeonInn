using System;
using System.Collections.Generic;
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

namespace DungeonInn.Application.UseCase
{
    public sealed class SellItemsUseCase
    {
        readonly IItemMasterRepository masterRepository;
        readonly IEventPublisher eventBus;
        readonly IGameClock gameClock;
        readonly PricePolicy pricePolicy = new();
        readonly ExchangeExecutor exchangeExecutor = new();

        [Inject]
        public SellItemsUseCase(
            IItemMasterRepository masterRepository,
            IEventPublisher eventBus,
            IGameClock gameClock)
        {
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            this.gameClock = gameClock ?? throw new ArgumentNullException(nameof(gameClock));
        }

        public SellItemsUseCase(IItemMasterRepository masterRepository, IEventPublisher eventBus)
            : this(masterRepository, eventBus, new NullGameClock())
        {
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
                    behavior.LifecycleState != AdventurerLifecycleState.Recovering &&
                    behavior.LifecycleState != AdventurerLifecycleState.WaitingForInn)
                {
                    continue;
                }

                SellItems(worldState.Guild, actor);
            }
        }

        void SellItems(AdventurerGuild guild, Actor actor)
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
                if (!TryFindSaleFacility(guild, itemMaster.Category, out var facility))
                {
                    continue;
                }

                var price = pricePolicy.CalculatePurchasePrice(new[] { stack }, masterRepository.ItemMasters);
                if (!facility.Inventory.Has(price) ||
                    !facility.Inventory.CanAdd(stack) ||
                    !actor.Inventory.CanAdd(price))
                {
                    continue;
                }

                var transaction = exchangeExecutor.Execute(
                    actor,
                    facility,
                    new[] { stack },
                    new[] { price },
                    gameClock.CurrentScheduleTick);
                guild.RecordTransaction(transaction);

                eventBus.Publish(new ItemSold(actor.Id, stack, price.Count, actor.Inventory.Gold));
            }
        }

        static bool TryFindSaleFacility(AdventurerGuild guild, ItemCategory itemCategory, out Facility facility)
        {
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

        sealed class NullGameClock : IGameClock
        {
            public int TotalScheduleTick => 0;
            public int CurrentScheduleTick => 0;
            public int CurrentDay => 0;
            public int CurrentTickOfDay => 0;
            public float ElapsedRealTimeSeconds => 0f;
            public float ElapsedGameTimeSeconds => 0f;
            public float TimeScale => 1f;
            public bool IsPaused => false;

            public void SetTimeScale(float timeScale)
            {
            }

            public void Pause()
            {
            }

            public void Resume()
            {
            }

            public GameClockAdvanceResult Advance(float unscaledDeltaTimeSeconds)
            {
                return new GameClockAdvanceResult(0, Array.Empty<int>());
            }
        }
    }
}
