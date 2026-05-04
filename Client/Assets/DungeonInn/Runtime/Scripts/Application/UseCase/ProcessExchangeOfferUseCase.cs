using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Character;
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
        /// <summary>
        /// 交換条件を検証し、要求品と報酬品を交換して取引履歴を記録する。
        /// </summary>
        public UniTask ExecuteAsync(
            AdventurerGuild guild,
            Character adventurer,
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

            if (!guild.Inventory.HasAll(exchangeOffer.RewardItems))
            {
                throw new InvalidOperationException("Guild does not have reward items.");
            }

            adventurer.Inventory.RemoveRange(exchangeOffer.RequestedItems);
            guild.Inventory.AddRange(exchangeOffer.RequestedItems);
            guild.Inventory.RemoveRange(exchangeOffer.RewardItems);
            adventurer.Inventory.AddRange(exchangeOffer.RewardItems);
            guild.RecordTransaction(
                new ExchangeTransaction(
                    Guid.NewGuid(),
                    adventurer.Id,
                    exchangeOffer.FacilityId,
                    exchangeOffer.RequestedItems,
                    exchangeOffer.RewardItems,
                    occurredAtTick));

            return UniTask.CompletedTask;
        }
    }
}
