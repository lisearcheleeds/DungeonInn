using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Actors.Lifecycle;
using DungeonInn.Application.Economy;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.World;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Commerce;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.Facilities
{
    public sealed class FacilityInteractionOrchestrator
    {
        readonly ChargeInnFeeUseCase chargeInnFeeUseCase;
        readonly FacilityNeedSelector facilityNeedSelector;
        readonly GetFacilityLineupUseCase getFacilityLineupUseCase;
        readonly IItemMasterRepository itemMasterRepository;
        readonly IGameClock gameClock;
        readonly IEventPublisher eventPublisher;
        readonly ActorProcessingCandidateService candidateService;
        readonly IWorldGameSettingsRepository worldGameSettingsRepository;
        readonly ExchangeExecutor exchangeExecutor = new();
        readonly PricePolicy pricePolicy = new();
        readonly List<ItemStack> sellBuffer = new();

        [Inject]
        public FacilityInteractionOrchestrator(
            ChargeInnFeeUseCase chargeInnFeeUseCase,
            FacilityNeedSelector facilityNeedSelector,
            GetFacilityLineupUseCase getFacilityLineupUseCase,
            IItemMasterRepository itemMasterRepository,
            IGameClock gameClock,
            IEventPublisher eventPublisher,
            ActorProcessingCandidateService candidateService,
            IWorldGameSettingsRepository worldGameSettingsRepository)
        {
            this.chargeInnFeeUseCase = chargeInnFeeUseCase ?? throw new ArgumentNullException(nameof(chargeInnFeeUseCase));
            this.facilityNeedSelector = facilityNeedSelector ?? throw new ArgumentNullException(nameof(facilityNeedSelector));
            this.getFacilityLineupUseCase = getFacilityLineupUseCase
                ?? throw new ArgumentNullException(nameof(getFacilityLineupUseCase));
            this.itemMasterRepository = itemMasterRepository ?? throw new ArgumentNullException(nameof(itemMasterRepository));
            this.gameClock = gameClock ?? throw new ArgumentNullException(nameof(gameClock));
            this.eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            this.candidateService = candidateService ?? throw new ArgumentNullException(nameof(candidateService));
            this.worldGameSettingsRepository =
                worldGameSettingsRepository ?? throw new ArgumentNullException(nameof(worldGameSettingsRepository));
        }

        public UniTask<FacilityInteractionResult> ExecuteAsync(IGameWorldState worldState, Actor actor, Facility facility)
        {
            var result = FacilityInteractionResult.Completed;
            switch (facility.Type)
            {
                case FacilityType.Inn:
                    result = UseInn(worldState.Guild, actor, facility);
                    break;
                case FacilityType.GeneralStore:
                    UseGeneralStore(worldState.Guild, actor, facility);
                    break;
                case FacilityType.EquipmentShop:
                    UseEquipmentShop(worldState.Guild, actor, facility);
                    break;
            }

            return UniTask.FromResult(result);
        }

        FacilityInteractionResult UseInn(AdventurerGuild guild, Actor actor, Facility facility)
        {
            var behavior = actor.RequireBehavior<AdventurerBehavior>();
            if (guild.HasActiveInnReservation(actor.Id))
            {
                return FacilityInteractionResult.StayingInside;
            }

            var hasQueuedActor = guild.TryPeekQueuedInnReservation(facility.Id, out var queuedActorId);
            if ((hasQueuedActor && !queuedActorId.Equals(actor.Id)) ||
                !guild.CanReserveInn(facility.Id))
            {
                guild.EnqueueInnReservation(actor, facility.Id);
                behavior.StartWaitingForInn(gameClock.CurrentDay);
                PublishWaitingForInn(actor, facility);
                eventPublisher.Publish(new ActorWaitingForInn(actor.Id, facility.Id));
                return FacilityInteractionResult.WaitingOutside;
            }

            chargeInnFeeUseCase.Execute(actor, guild, facility);
            guild.ReserveInn(Guid.NewGuid(), actor, facility.Id, gameClock.CurrentScheduleTick);
            behavior.ClearWaitingForInn();
            behavior.ChangeLifecycleState(AdventurerLifecycleState.Recovering);
            candidateService.MarkRecoveryCandidate(actor.Id);
            eventPublisher.Publish(new ActorReservedInn(actor.Id, facility.Id));
            return FacilityInteractionResult.StayingInside;
        }

        void PublishWaitingForInn(Actor actor, Facility facility)
        {
            var innBalanceSettings = worldGameSettingsRepository.GetInnBalanceSettings();
            eventPublisher.Publish(new InnSatisfactionChanged(
                actor.Id,
                innBalanceSettings.WaitingSatisfactionDelta,
                InnSatisfactionChangeReason.WaitingForInn));
            eventPublisher.Publish(new ActorAiDecisionRecorded(
                actor.Id,
                AiDecisionType.WaitForInn,
                AiDecisionReasonType.NoVacantInnRoom,
                default,
                facility.Id,
                0,
                0,
                0,
                0));
        }

        void UseGeneralStore(AdventurerGuild guild, Actor actor, Facility facility)
        {
            SellMatchingItems(guild, actor, facility, IsGeneralStoreSellable);
            BuyPotionIfNeeded(guild, actor, facility);
        }

        void UseEquipmentShop(AdventurerGuild guild, Actor actor, Facility facility)
        {
            SellMatchingItems(guild, actor, facility, itemMaster =>
                IsEquipmentItem(itemMaster) && !actor.Equipment.IsEquipped(itemMaster.Id));
            BuyBetterWeaponIfAvailable(guild, actor, facility);
        }

        void SellMatchingItems(
            AdventurerGuild guild,
            Actor actor,
            Facility facility,
            Func<ItemMaster, bool> predicate)
        {
            sellBuffer.Clear();
            foreach (var slot in actor.Inventory.Slots)
            {
                if (!itemMasterRepository.ItemMasters.TryGetValue(slot.ItemId, out var itemMaster))
                {
                    continue;
                }

                if (predicate(itemMaster))
                {
                    sellBuffer.Add(new ItemStack(slot.ItemId, slot.Count));
                }
            }

            for (var i = 0; i < sellBuffer.Count; i++)
            {
                var stack = sellBuffer[i];
                var price = pricePolicy.CalculatePurchasePrice(stack, itemMasterRepository.ItemMasters);
                if (!facility.Inventory.Has(price) ||
                    !facility.Inventory.CanAdd(stack) ||
                    !actor.Inventory.CanAdd(price))
                {
                    continue;
                }

                var transaction = exchangeExecutor.Execute(actor, facility, stack, price, gameClock.CurrentScheduleTick);
                guild.RecordTransaction(transaction);
                eventPublisher.Publish(new ItemSold(actor.Id, stack, price.Count, actor.Inventory.Gold));
                candidateService.MarkInventoryChanged(actor.Id);
            }
        }

        void BuyPotionIfNeeded(AdventurerGuild guild, Actor actor, Facility facility)
        {
            var count = actor.Inventory.ItemCounts.TryGetValue(SpecialItemIds.Potion, out var current) ? current : 0;
            var buyCount = Math.Max(0, 2 - count);
            for (var i = 0; i < buyCount; i++)
            {
                if (!TryBuyItem(guild, actor, facility, SpecialItemIds.Potion))
                {
                    return;
                }
            }
        }

        void BuyBetterWeaponIfAvailable(AdventurerGuild guild, Actor actor, Facility facility)
        {
            if (!facilityNeedSelector.TryFindBetterAffordableWeapon(actor, facility, out var itemId))
            {
                return;
            }

            if (!TryBuyItem(guild, actor, facility, itemId))
            {
                return;
            }

            var equipmentMaster = itemMasterRepository.GetEquipmentMaster(itemId);
            var weaponMaster = itemMasterRepository.GetWeaponMaster(itemId);
            var currentItemId = actor.Equipment.GetEquippedItemId(EquipmentSlot.Weapon);
            actor.RemoveItem(new ItemStack(itemId, 1));
            if (currentItemId.HasValue)
            {
                actor.Unequip(EquipmentSlot.Weapon);
                actor.GainItem(new ItemStack(currentItemId.Value, 1));
            }

            actor.Equip(equipmentMaster, weaponMaster);
            eventPublisher.Publish(new EquipmentChanged(actor.Id, EquipmentSlot.Weapon, currentItemId, itemId));
            candidateService.MarkInventoryChanged(actor.Id);
        }

        bool TryBuyItem(AdventurerGuild guild, Actor actor, Facility facility, int itemId)
        {
            if (!IsLineupItem(facility, itemId))
            {
                return false;
            }

            var itemMaster = itemMasterRepository.GetItemMaster(itemId);
            var item = new ItemStack(itemId, 1);
            var price = new ItemStack(SpecialItemIds.Money, itemMaster.BasePrice * Math.Max(1, facility.Level));
            if (!actor.Inventory.Has(price) ||
                !facility.Inventory.Has(item) ||
                !actor.Inventory.CanAdd(item) ||
                !facility.Inventory.CanAdd(price))
            {
                return false;
            }

            var transaction = exchangeExecutor.Execute(actor, facility, price, item, gameClock.CurrentScheduleTick);
            guild.RecordTransaction(transaction);
            candidateService.MarkInventoryChanged(actor.Id);
            return true;
        }

        bool IsLineupItem(Facility facility, int itemId)
        {
            var lineup = getFacilityLineupUseCase.Execute(facility.Type, facility.Level);
            for (var i = 0; i < lineup.Count; i++)
            {
                if (lineup[i].ItemId == itemId)
                {
                    return true;
                }
            }

            return false;
        }

        static bool IsGeneralStoreSellable(ItemMaster itemMaster)
        {
            if (!itemMaster.CanTrade)
            {
                return false;
            }

            if (itemMaster.Id == SpecialItemIds.Money ||
                itemMaster.Id == SpecialItemIds.Potion ||
                itemMaster.HasTag(ItemTag.Recovery) ||
                itemMaster.HasTag(ItemTag.ManaRecovery))
            {
                return false;
            }

            return !IsEquipmentItem(itemMaster);
        }

        static bool IsEquipmentItem(ItemMaster itemMaster)
        {
            return itemMaster.HasTag(ItemTag.Weapon) ||
                itemMaster.HasTag(ItemTag.Armor) ||
                itemMaster.HasTag(ItemTag.Accessory);
        }
    }
}
