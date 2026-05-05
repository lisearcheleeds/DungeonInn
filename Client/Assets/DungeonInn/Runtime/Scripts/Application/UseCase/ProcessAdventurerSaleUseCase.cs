using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Commerce;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;

namespace DungeonInn.Application.UseCase
{
    /// <summary>
    /// 冒険者がアイテムや装備をギルド施設へ売却する処理を行うユースケース。
    /// </summary>
    public sealed class ProcessAdventurerSaleUseCase
    {
        readonly PricePolicy pricePolicy = new();

        /// <summary>
        /// 売却品の買取価格を計算し、冒険者とギルドの在庫交換と取引履歴を更新する。
        /// </summary>
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

            var price = pricePolicy.CalculatePurchasePrice(soldItems, itemMasters);
            if (!guild.Inventory.Has(price))
            {
                throw new InvalidOperationException("Guild does not have enough payment item.");
            }

            guild.Inventory.Remove(price);
            adventurer.Inventory.Add(price);
            adventurer.Inventory.RemoveRange(soldItems);
            guild.Inventory.AddRange(soldItems);
            guild.RecordTransaction(
                new ExchangeTransaction(
                    Guid.NewGuid(),
                    adventurer.Id,
                    facilityId,
                    soldItems,
                    new[] { price },
                    occurredAtTick));

            return UniTask.CompletedTask;
        }
    }
}
