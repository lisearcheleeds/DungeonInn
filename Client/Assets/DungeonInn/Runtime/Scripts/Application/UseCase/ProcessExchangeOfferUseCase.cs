using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Commerce;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;

namespace DungeonInn.Application.UseCase
{
    /// <summary>
    /// 施設の交換項目に基づいて、冒険者とギルド間のアイテム交換を処理するユースケース。
    /// </summary>
    public sealed class ProcessExchangeOfferUseCase
    {
        readonly ExchangeExecutor exchangeExecutor = new();

        /// <summary>
        /// 交換条件を検証し、要求品と報酬品を交換して取引履歴を記録する。
        /// </summary>
        public UniTask ExecuteAsync(
            AdventurerGuild guild,
            Actor adventurer,
            ExchangeOffer exchangeOffer,
            int occurredAtTick)
        {
            if (exchangeOffer == null)
            {
                throw new ArgumentNullException(nameof(exchangeOffer));
            }

            if (!exchangeOffer.IsActive)
            {
                throw new InvalidOperationException("Exchange offer is inactive.");
            }

            if (!adventurer.Inventory.HasAll(exchangeOffer.RequestedItems))
            {
                throw new InvalidOperationException("Adventurer does not have requested items.");
            }

            var facility = guild.GetFacility(exchangeOffer.FacilityId);
            if (!facility.Inventory.HasAll(exchangeOffer.RewardItems))
            {
                throw new InvalidOperationException("Facility does not have reward items.");
            }

            var transaction = exchangeExecutor.Execute(
                adventurer,
                facility,
                exchangeOffer.RequestedItems,
                exchangeOffer.RewardItems,
                occurredAtTick);
            guild.RecordTransaction(transaction);

            return UniTask.CompletedTask;
        }
    }
}
