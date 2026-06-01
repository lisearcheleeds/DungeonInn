using System;
using DungeonInn.Application.Economy;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Commerce;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.Facilities
{
    public sealed class FacilityNeedSelector
    {
        readonly IItemMasterRepository itemMasterRepository;
        readonly GetFacilityLineupUseCase getFacilityLineupUseCase;
        readonly PricePolicy pricePolicy = new();

        [Inject]
        public FacilityNeedSelector(
            IItemMasterRepository itemMasterRepository,
            GetFacilityLineupUseCase getFacilityLineupUseCase)
        {
            this.itemMasterRepository = itemMasterRepository ?? throw new ArgumentNullException(nameof(itemMasterRepository));
            this.getFacilityLineupUseCase = getFacilityLineupUseCase
                ?? throw new ArgumentNullException(nameof(getFacilityLineupUseCase));
        }

        public bool TrySelectFacility(AdventurerGuild guild, Actor actor, out Facility facility)
        {
            if (TryFindFacility(guild, FacilityType.GeneralStore, out facility) &&
                NeedsGeneralStore(actor, facility))
            {
                return true;
            }

            if (TryFindFacility(guild, FacilityType.EquipmentShop, out facility) &&
                NeedsEquipmentShop(actor, facility))
            {
                return true;
            }

            if (NeedsInn(guild, actor) && TryFindFacility(guild, FacilityType.Inn, out facility))
            {
                return true;
            }

            facility = null;
            return false;
        }

        public bool TryFindBetterAffordableWeapon(Actor actor, Facility facility, out int itemId)
        {
            var currentScore = GetCurrentWeaponScore(actor);
            var lineup = getFacilityLineupUseCase.Execute(facility.Type, facility.Level);
            itemId = 0;
            var bestScore = currentScore;
            for (var i = 0; i < lineup.Count; i++)
            {
                var candidateItemId = lineup[i].ItemId;
                if (!itemMasterRepository.WeaponMasters.TryGetValue(candidateItemId, out var weaponMaster))
                {
                    continue;
                }

                if (weaponMaster.Attack <= bestScore ||
                    !CanBuyItem(actor, facility, candidateItemId))
                {
                    continue;
                }

                itemId = candidateItemId;
                bestScore = weaponMaster.Attack;
            }

            return 0 < itemId;
        }

        bool NeedsInn(AdventurerGuild guild, Actor actor)
        {
            return actor.Hp < actor.Params.MaxHp &&
                !guild.HasActiveInnReservation(actor.Id) &&
                !guild.HasQueuedInnReservation(actor.Id);
        }

        bool NeedsGeneralStore(Actor actor, Facility facility)
        {
            if (CountItem(actor, SpecialItemIds.Potion) < 2 &&
                CanBuyItem(actor, facility, SpecialItemIds.Potion))
            {
                return true;
            }

            return CanSellMatchingItems(actor, facility, IsGeneralStoreSellable);
        }

        bool NeedsEquipmentShop(Actor actor, Facility facility)
        {
            if (CanSellMatchingItems(
                actor,
                facility,
                itemMaster => IsEquipmentItem(itemMaster) && !actor.Equipment.IsEquipped(itemMaster.Id)))
            {
                return true;
            }

            return TryFindBetterAffordableWeapon(actor, facility, out _);
        }

        bool CanSellMatchingItems(
            Actor actor,
            Facility facility,
            Func<ItemMaster, bool> predicate)
        {
            foreach (var slot in actor.Inventory.Slots)
            {
                if (!itemMasterRepository.ItemMasters.TryGetValue(slot.ItemId, out var itemMaster))
                {
                    continue;
                }

                if (!predicate(itemMaster))
                {
                    continue;
                }

                var stack = new ItemStack(slot.ItemId, slot.Count);
                var price = pricePolicy.CalculatePurchasePrice(stack, itemMasterRepository.ItemMasters);
                if (facility.Inventory.Has(price) &&
                    facility.Inventory.CanAdd(stack) &&
                    actor.Inventory.CanAdd(price))
                {
                    return true;
                }
            }

            return false;
        }

        bool CanBuyItem(Actor actor, Facility facility, int itemId)
        {
            if (!IsLineupItem(facility, itemId))
            {
                return false;
            }

            var itemMaster = itemMasterRepository.GetItemMaster(itemId);
            var item = new ItemStack(itemId, 1);
            var price = new ItemStack(SpecialItemIds.Money, itemMaster.BasePrice * Math.Max(1, facility.Level));
            return actor.Inventory.Has(price) &&
                facility.Inventory.Has(item) &&
                actor.Inventory.CanAdd(item) &&
                facility.Inventory.CanAdd(price);
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

        static int CountItem(Actor actor, int itemId)
        {
            return actor.Inventory.ItemCounts.TryGetValue(itemId, out var count) ? count : 0;
        }

        int GetCurrentWeaponScore(Actor actor)
        {
            var itemId = actor.Equipment.GetEquippedItemId(EquipmentSlot.Weapon);
            if (!itemId.HasValue ||
                !itemMasterRepository.WeaponMasters.TryGetValue(itemId.Value, out var weaponMaster))
            {
                return 0;
            }

            return weaponMaster.Attack;
        }

        static bool TryFindFacility(AdventurerGuild guild, FacilityType facilityType, out Facility facility)
        {
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
