using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Character;
using DungeonInn.Domain.Commerce;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;

namespace DungeonInn.Application.UseCase
{
    /// <summary>
    /// 冒険者による施設利用の支払い、効果適用、取引記録を処理するユースケース。
    /// </summary>
    public sealed class ProcessFacilityUsageUseCase
    {
        /// <summary>
        /// 施設利用リクエストを処理し、冒険者とギルドの状態を更新する。
        /// </summary>
        public UniTask ExecuteAsync(
            AdventurerGuild guild,
            Character adventurer,
            Guid facilityId,
            FacilityUsageRequest request,
            int occurredAtTick)
        {
            var facility = guild.GetFacility(facilityId);
            var price = facility.CalculateUsagePrice(request);
            if (!adventurer.Inventory.Has(price))
            {
                throw new InvalidOperationException("Adventurer does not have enough payment item.");
            }

            if (IsPurchase(request) && !guild.Inventory.HasAll(request.PurchasedItems))
            {
                throw new InvalidOperationException("Guild does not have purchased items.");
            }

            adventurer.Inventory.Remove(price);
            guild.Inventory.Add(price);
            ApplyFacilityEffect(guild, adventurer, facility, request);
            RecordTransaction(guild, adventurer, facilityId, request, price, occurredAtTick);

            return UniTask.CompletedTask;
        }

        static void ApplyFacilityEffect(
            AdventurerGuild guild,
            Character adventurer,
            Facility facility,
            FacilityUsageRequest request)
        {
            switch (request.UsageType)
            {
                case FacilityUsageType.Rest:
                    adventurer.Recover(
                        facility.Quality * 10,
                        facility.Quality * 3,
                        facility.Quality * 5,
                        facility.Quality,
                        facility.Quality);
                    return;
                case FacilityUsageType.BuyItem:
                case FacilityUsageType.BuyEquipment:
                    guild.Inventory.RemoveRange(request.PurchasedItems);
                    adventurer.Inventory.AddRange(request.PurchasedItems);
                    return;
                case FacilityUsageType.Meal:
                    adventurer.Recover(
                        0,
                        0,
                        facility.Quality * 2,
                        facility.Quality * 2,
                        0);
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(request));
            }
        }

        static void RecordTransaction(
            AdventurerGuild guild,
            Character adventurer,
            Guid facilityId,
            FacilityUsageRequest request,
            ItemStack price,
            int occurredAtTick)
        {
            switch (request.UsageType)
            {
                case FacilityUsageType.Rest:
                case FacilityUsageType.Meal:
                    guild.RecordTransaction(
                        new ExchangeTransaction(
                            Guid.NewGuid(),
                            adventurer.Id,
                            facilityId,
                            new[] { price },
                            Array.Empty<ItemStack>(),
                            occurredAtTick));
                    return;
                case FacilityUsageType.BuyItem:
                case FacilityUsageType.BuyEquipment:
                    guild.RecordTransaction(
                        new ExchangeTransaction(
                            Guid.NewGuid(),
                            adventurer.Id,
                            facilityId,
                            new[] { price },
                            request.PurchasedItems,
                            occurredAtTick));
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(request));
            }
        }

        static bool IsPurchase(FacilityUsageRequest request)
        {
            return request.UsageType == FacilityUsageType.BuyItem || request.UsageType == FacilityUsageType.BuyEquipment;
        }
    }
}
