using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Commerce;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;
using DungeonInn.Master;

namespace DungeonInn.Application.Economy
{
    /// <summary>
    /// 冒険老E��アイチE��めE��E��をギルド施設へ売却する処琁E��行うユースケース、E    /// </summary>
    public sealed class ProcessAdventurerSaleUseCase
    {
        readonly PricePolicy pricePolicy = new();
        readonly ExchangeExecutor exchangeExecutor = new();

        /// <summary>
        /// 売却品�E買取価格を計算し、�E険老E��ギルド�E在庫交換と取引履歴を更新する、E        /// </summary>
        public UniTask ExecuteAsync(
            AdventurerGuild guild,
            Actor adventurer,
            Guid facilityId,
            IReadOnlyList<ItemStack> soldItems,
            IReadOnlyDictionary<int, ItemMaster> itemMasters,
            int occurredAtTick)
        {
            if (!adventurer.Inventory.HasAll(soldItems))
            {
                throw new InvalidOperationException("Adventurer does not have sold items.");
            }

            var facility = guild.GetFacility(facilityId);
            var price = pricePolicy.CalculatePurchasePrice(soldItems, itemMasters);
            if (!facility.Inventory.Has(price))
            {
                throw new InvalidOperationException("Facility does not have enough payment item.");
            }

            var transaction = exchangeExecutor.Execute(
                adventurer,
                facility,
                soldItems,
                new[] { price },
                occurredAtTick);
            guild.RecordTransaction(transaction);

            return UniTask.CompletedTask;
        }
    }
}
