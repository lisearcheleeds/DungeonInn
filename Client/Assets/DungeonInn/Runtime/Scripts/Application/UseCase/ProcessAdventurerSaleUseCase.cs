using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Character;
using DungeonInn.Domain.Commerce;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;

namespace DungeonInn.Application.UseCase
{
    public sealed class ProcessAdventurerSaleUseCase
    {
        readonly PricePolicy pricePolicy = new();

        public UniTask ExecuteAsync(
            AdventurerGuild guild,
            Character adventurer,
            Guid facilityId,
            IReadOnlyList<ItemStack> soldItems,
            IReadOnlyDictionary<int, ItemDefinition> definitions,
            int occurredAtTick)
        {
            if (!adventurer.Inventory.HasAll(soldItems))
            {
                throw new InvalidOperationException("Adventurer does not have sold items.");
            }

            var price = pricePolicy.CalculatePurchasePrice(soldItems, definitions);
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
