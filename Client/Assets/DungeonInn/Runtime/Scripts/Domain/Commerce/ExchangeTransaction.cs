using System;
using System.Collections.Generic;
using DungeonInn.Domain.Item;

namespace DungeonInn.Domain.Commerce
{
    public sealed class ExchangeTransaction
    {
        public Guid Id { get; }
        public Guid InitiatorId { get; }
        public Guid CounterpartyId { get; }
        public IReadOnlyList<ItemStack> InitiatorItems { get; }
        public IReadOnlyList<ItemStack> CounterpartyItems { get; }
        public int OccurredAtTick { get; }

        public ExchangeTransaction(
            Guid id,
            Guid initiatorId,
            Guid counterpartyId,
            IReadOnlyList<ItemStack> initiatorItems,
            IReadOnlyList<ItemStack> counterpartyItems,
            int occurredAtTick)
        {
            Id = id;
            InitiatorId = initiatorId;
            CounterpartyId = counterpartyId;
            InitiatorItems = initiatorItems ?? throw new ArgumentNullException(nameof(initiatorItems));
            CounterpartyItems = counterpartyItems ?? throw new ArgumentNullException(nameof(counterpartyItems));
            OccurredAtTick = occurredAtTick;
        }
    }
}
