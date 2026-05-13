using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Commerce;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;
using VContainer;

namespace DungeonInn.Application.Facilities
{
    /// <summary>
    /// 冒険老E��よる施設利用の支払い、効果適用、取引記録を�E琁E��るユースケース、E    /// </summary>
    public sealed class ProcessFacilityUsageUseCase
    {
        readonly IEventPublisher eventBus;
        readonly ExchangeExecutor exchangeExecutor = new();

        [Inject]
        public ProcessFacilityUsageUseCase(IEventPublisher eventBus)
        {
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        /// <summary>
        /// 施設利用リクエストを処琁E��、�E険老E��ギルド�E状態を更新する、E        /// </summary>
        public UniTask ExecuteAsync(
            AdventurerGuild guild,
            Actor adventurer,
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

            if (IsPurchase(request) && !facility.Inventory.HasAll(request.PurchasedItems))
            {
                throw new InvalidOperationException("Facility does not have purchased items.");
            }

            var transaction = Exchange(adventurer, facility, request, price, occurredAtTick);
            ApplyFacilityEffect(adventurer, facility, request);
            guild.RecordTransaction(transaction);
            eventBus.Publish(new ActorAiDecisionRecorded(
                adventurer.Id,
                AiDecisionType.UseFacility,
                AiDecisionReasonType.FacilityUsageRequest,
                default,
                facilityId,
                0,
                0,
                0,
                0));

            return UniTask.CompletedTask;
        }

        static void ApplyFacilityEffect(
            Actor adventurer,
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

        ExchangeTransaction Exchange(
            Actor adventurer,
            Facility facility,
            FacilityUsageRequest request,
            ItemStack price,
            int occurredAtTick)
        {
            switch (request.UsageType)
            {
                case FacilityUsageType.Rest:
                case FacilityUsageType.Meal:
                    return exchangeExecutor.Execute(
                        adventurer,
                        facility,
                        new[] { price },
                        Array.Empty<ItemStack>(),
                        occurredAtTick);
                case FacilityUsageType.BuyItem:
                case FacilityUsageType.BuyEquipment:
                    return exchangeExecutor.Execute(
                        adventurer,
                        facility,
                        new[] { price },
                        request.PurchasedItems,
                        occurredAtTick);
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
