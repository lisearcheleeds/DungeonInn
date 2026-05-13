using System;
using System.Collections.Generic;
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

        public ExchangeTransaction Execute(
            IExchangeParticipant initiator,
            IExchangeParticipant counterparty,
            ItemStack initiatorItem,
            ItemStack counterpartyItem,
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

            if (!initiator.Has(initiatorItem))
            {
                throw new InvalidOperationException("Initiator does not have enough exchange items.");
            }

            if (!counterparty.Has(counterpartyItem))
            {
                throw new InvalidOperationException("Counterparty does not have enough exchange items.");
            }

            if (!initiator.CanAddAfterRemoving(initiatorItem, counterpartyItem))
            {
                throw new InvalidOperationException("Initiator cannot receive exchange items.");
            }

            if (!counterparty.CanAddAfterRemoving(counterpartyItem, initiatorItem))
            {
                throw new InvalidOperationException("Counterparty cannot receive exchange items.");
            }

            initiator.Remove(initiatorItem);
            counterparty.Remove(counterpartyItem);
            initiator.Add(counterpartyItem);
            counterparty.Add(initiatorItem);

            return new ExchangeTransaction(
                Guid.NewGuid(),
                initiator.Id,
                counterparty.Id,
                new[] { initiatorItem },
                new[] { counterpartyItem },
                occurredAtTick);
        }

        static IReadOnlyList<ItemStack> NormalizeItems(IReadOnlyList<ItemStack> itemStacks)
        {
            if (itemStacks == null || itemStacks.Count == 0)
            {
                return Array.Empty<ItemStack>();
            }

            var normalizedItems = new List<ItemStack>();
            for (var i = 0; i < itemStacks.Count; i++)
            {
                var itemStack = itemStacks[i];
                var existingIndex = FindIndex(normalizedItems, itemStack.ItemId);
                if (existingIndex < 0)
                {
                    normalizedItems.Add(itemStack);
                    continue;
                }

                var existing = normalizedItems[existingIndex];
                normalizedItems[existingIndex] = new ItemStack(
                    existing.ItemId,
                    existing.Count + itemStack.Count);
            }

            return normalizedItems.ToArray();
        }

        static int FindIndex(IReadOnlyList<ItemStack> itemStacks, int itemId)
        {
            for (var i = 0; i < itemStacks.Count; i++)
            {
                if (itemStacks[i].ItemId == itemId)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
