using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Domain.Item;

namespace DungeonInn.Domain.Commerce
{
    public sealed class ExchangeExecutor
    {
        public ExchangeTransaction Execute(
            IExchangeParticipant initiator,
            IExchangeParticipant counterparty,
            IReadOnlyList<ItemStack> initiatorItems,
            IReadOnlyList<ItemStack> counterpartyItems,
            int occurredAtTick)
        {
            if (initiator == null)
            {
                throw new ArgumentNullException(nameof(initiator));
            }

            if (counterparty == null)
            {
                throw new ArgumentNullException(nameof(counterparty));
            }

            var normalizedInitiatorItems = NormalizeItems(initiatorItems);
            var normalizedCounterpartyItems = NormalizeItems(counterpartyItems);

            if (!initiator.HasAll(normalizedInitiatorItems))
            {
                throw new InvalidOperationException("Initiator does not have enough exchange items.");
            }

            if (!counterparty.HasAll(normalizedCounterpartyItems))
            {
                throw new InvalidOperationException("Counterparty does not have enough exchange items.");
            }

            if (!initiator.CanAddAfterRemoving(normalizedInitiatorItems, normalizedCounterpartyItems))
            {
                throw new InvalidOperationException("Initiator cannot receive exchange items.");
            }

            if (!counterparty.CanAddAfterRemoving(normalizedCounterpartyItems, normalizedInitiatorItems))
            {
                throw new InvalidOperationException("Counterparty cannot receive exchange items.");
            }

            initiator.RemoveRange(normalizedInitiatorItems);
            counterparty.RemoveRange(normalizedCounterpartyItems);
            initiator.AddRange(normalizedCounterpartyItems);
            counterparty.AddRange(normalizedInitiatorItems);

            return new ExchangeTransaction(
                Guid.NewGuid(),
                initiator.Id,
                counterparty.Id,
                normalizedInitiatorItems,
                normalizedCounterpartyItems,
                occurredAtTick);
        }

        static IReadOnlyList<ItemStack> NormalizeItems(IReadOnlyList<ItemStack> itemStacks)
        {
            if (itemStacks == null || itemStacks.Count == 0)
            {
                return Array.Empty<ItemStack>();
            }

            return itemStacks
                .GroupBy(itemStack => itemStack.ItemId)
                .Select(group => new ItemStack(group.Key, group.Sum(itemStack => itemStack.Count)))
                .ToArray();
        }
    }
}
